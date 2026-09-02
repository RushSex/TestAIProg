using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Класс управления состоянием здоровья выжившего.
/// Обрабатывает переходы между состояниями: Healthy -> Injured -> Downed.
/// </summary>
public class SurvivorHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float damageThresholdInjured = 50f; // Порог для состояния Injured
    
    [Header("State Effects")]
    [SerializeField] private float injuredSpeedPenalty = 0.85f; // -15% скорости
    [SerializeField] private GameObject bloodTrailPrefab;
    [SerializeField] private AudioClip injurySound;
    [SerializeField] private AudioClip downedSound;
    
    // Текущее состояние здоровья
    private NetworkVariable<SurvivorHealthState> healthState = new NetworkVariable<SurvivorHealthState>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );
    
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );
    
    private AudioSource audioSource;
    private ParticleSystem bloodTrailParticles;
    private BaseCharacter baseCharacter;
    
    public SurvivorHealthState CurrentState => healthState.Value;
    public bool IsAlive => healthState.Value != SurvivorHealthState.Downed || currentHealth.Value > 0;
    
    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
        baseCharacter = GetComponent<BaseCharacter>();
        
        if (bloodTrailPrefab != null)
        {
            var trailObj = Instantiate(bloodTrailPrefab, transform);
            bloodTrailParticles = trailObj.GetComponent<ParticleSystem>();
            if (bloodTrailParticles != null)
            {
                bloodTrailParticles.Stop();
            }
        }
    }
    
    private void Start()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            healthState.Value = SurvivorHealthState.Healthy;
        }
        
        // Подписка на изменения состояния для визуальных эффектов
        healthState.OnValueChanged += OnHealthStateChanged;
    }
    
    /// <summary>
    /// Нанесение урона выжившему.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float damage, BaseCharacter.DamageSource source)
    {
        if (!IsServer) return;
        
        if (healthState.Value == SurvivorHealthState.Downed)
        {
            // Если уже в состоянии Downed, дополнительный урон может привести к смерти
            HandleDeath();
            return;
        }
        
        currentHealth.Value -= damage;
        
        if (currentHealth.Value <= 0)
        {
            healthState.Value = SurvivorHealthState.Downed;
            HandleDowned();
        }
        else if (currentHealth.Value < damageThresholdInjured && healthState.Value == SurvivorHealthState.Healthy)
        {
            healthState.Value = SurvivorHealthState.Injured;
            HandleInjured();
        }
    }
    
    /// <summary>
    /// Лечение выжившего.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void HealServerRpc(float amount)
    {
        if (!IsServer) return;
        
        if (healthState.Value == SurvivorHealthState.Downed)
        {
            // Восстановление из состояния Downed требует полного лечения
            healthState.Value = SurvivorHealthState.Injured;
            currentHealth.Value = damageThresholdInjured;
        }
        else
        {
            currentHealth.Value = Mathf.Min(currentHealth.Value + amount, maxHealth);
            
            if (currentHealth.Value >= damageThresholdInjured && healthState.Value == SurvivorHealthState.Injured)
            {
                healthState.Value = SurvivorHealthState.Healthy;
            }
        }
    }
    
    private void HandleInjured()
    {
        // Эффекты для состояния Injured
        if (audioSource != null && injurySound != null)
        {
            audioSource.PlayOneShot(injurySound);
        }
        
        // Включение следа крови
        if (bloodTrailParticles != null)
        {
            bloodTrailParticles.Play();
        }
        
        // Уведомление о изменении скорости (через событие или прямой вызов)
        UpdateMovementSpeed();
    }
    
    private void HandleDowned()
    {
        // Эффекты для состояния Downed
        if (audioSource != null && downedSound != null)
        {
            audioSource.PlayOneShot(downedSound);
        }
        
        // Остановка следа крови
        if (bloodTrailParticles != null)
        {
            bloodTrailParticles.Stop();
        }
        
        // Выживший теперь может только ползти
        UpdateMovementSpeed();
    }
    
    private void HandleDeath()
    {
        // Логика смерти выжившего
        // Маньяк получает очки, выживший выбывает из матча
        GameManager.Instance?.OnSurvivorEliminated(gameObject);
    }
    
    private void UpdateMovementSpeed()
    {
        // Здесь можно изменить скорость персонажа через BaseCharacter
        // Реализация зависит от конкретной архитектуры
    }
    
    private void OnHealthStateChanged(SurvivorHealthState previous, SurvivorHealthState current)
    {
        if (!IsOwner) return;
        
        // Обновление UI и эффектов на клиенте
        UpdateVisualEffects(current);
    }
    
    private void UpdateVisualEffects(SurvivorHealthState state)
    {
        switch (state)
        {
            case SurvivorHealthState.Healthy:
                if (bloodTrailParticles != null)
                {
                    bloodTrailParticles.Stop();
                }
                break;
                
            case SurvivorHealthState.Injured:
                if (bloodTrailParticles != null)
                {
                    bloodTrailParticles.Play();
                }
                break;
                
            case SurvivorHealthState.Downed:
                if (bloodTrailParticles != null)
                {
                    bloodTrailParticles.Stop();
                }
                break;
        }
    }
    
    /// <summary>
    /// Проверка, может ли этот выживший помочь другому выжившему.
    /// </summary>
    public bool CanHelpOthers()
    {
        return healthState.Value == SurvivorHealthState.Healthy || 
               healthState.Value == SurvivorHealthState.Injured;
    }
    
    public override void OnDestroy()
    {
        base.OnDestroy();
        healthState.OnValueChanged -= OnHealthStateChanged;
    }
}
