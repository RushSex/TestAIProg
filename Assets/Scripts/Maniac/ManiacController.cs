using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Класс управления маньяком (The Butcher).
/// Реализует уникальные способности: бензопила, крюк-гарпун.
/// </summary>
[RequireComponent(typeof(BaseCharacter))]
public class ManiacController : NetworkBehaviour
{
    [Header("Maniac Stats")]
    [SerializeField] private float chainsawDamage = 100f; // Мгновенный критический урон
    [SerializeField] private float hookRange = 15f; // Дальность броска крюка
    [SerializeField] private float hookPullSpeed = 8f; // Скорость притягивания выжившего
    [SerializeField] private float hookCooldown = 3f; // Перезарядка крюка
    
    [Header("Audio & Effects")]
    [SerializeField] private AudioClip chainsawStartSound;
    [SerializeField] private AudioClip chainsawLoopSound;
    [SerializeField] private AudioClip hookThrowSound;
    [SerializeField] private GameObject hookProjectilePrefab;
    [SerializeField] private ParticleSystem chainsawSparks;
    
    [Header("Terror Radius")]
    [SerializeField] private float terrorRadiusBase = 20f; // Базовый радиус террора
    [SerializeField] private float terrorRadiusChainsaw = 30f; // Радиус при активной бензопиле
    
    private BaseCharacter baseCharacter;
    private AudioSource audioSource;
    private bool isChainsawActive = false;
    private bool isHookReady = true;
    private float currentTerrorRadius;
    
    // NetworkVariables
    private NetworkVariable<bool> networkChainsawActive = new NetworkVariable<bool>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );
    
    private NetworkVariable<bool> networkHookReady = new NetworkVariable<bool>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );
    
    public bool IsChainsawActive => networkChainsawActive.Value;
    public float CurrentTerrorRadius => currentTerrorRadius;
    
    protected override void Awake()
    {
        base.Awake();
        baseCharacter = GetComponent<BaseCharacter>();
        audioSource = GetComponent<AudioSource>();
        
        currentTerrorRadius = terrorRadiusBase;
    }
    
    private void Start()
    {
        if (IsServer)
        {
            networkChainsawActive.Value = false;
            networkHookReady.Value = true;
        }
        
        networkChainsawActive.OnValueChanged += OnChainsawStateChanged;
    }
    
    /// <summary>
    /// Активация/деактивация бензопилы.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ToggleChainsawServerRpc()
    {
        if (!IsServer) return;
        
        isChainsawActive = !isChainsawActive;
        networkChainsawActive.Value = isChainsawActive;
        
        if (isChainsawActive)
        {
            currentTerrorRadius = terrorRadiusChainsaw;
            PlayChainsawStart();
        }
        else
        {
            currentTerrorRadius = terrorRadiusBase;
            StopChainsawLoop();
        }
    }
    
    /// <summary>
    /// Атака бензопилой (ближний бой).
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ChainsawAttackServerRpc()
    {
        if (!IsServer || !isChainsawActive) return;
        
        // Проверка попадания по выжившим в радиусе атаки
        Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward * 2f, 1.5f);
        
        foreach (Collider hit in hitColliders)
        {
            SurvivorHealth survivorHealth = hit.GetComponent<SurvivorHealth>();
            if (survivorHealth != null)
            {
                survivorHealth.TakeDamageServerRpc(chainsawDamage, BaseCharacter.DamageSource.Chainsaw);
            }
        }
    }
    
    /// <summary>
    /// Бросок крюка-гарпуна в выжившего.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ThrowHookServerRpc(Vector3 direction)
    {
        if (!IsServer || !networkHookReady.Value) return;
        
        networkHookReady.Value = false;
        
        // Raycast для поиска выжившего в направлении броска
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 1.5f, direction, out hit, hookRange))
        {
            SurvivorHealth survivorHealth = hit.collider.GetComponent<SurvivorHealth>();
            if (survivorHealth != null && survivorHealth.CurrentState != SurvivorHealthState.Downed)
            {
                // Притягивание выжившего
                StartCoroutine(PullSurvivor(hit.collider.gameObject));
                
                if (audioSource != null && hookThrowSound != null)
                {
                    audioSource.PlayOneShot(hookThrowSound);
                }
            }
        }
        
        // Перезарядка крюка
        Invoke(nameof(ResetHook), hookCooldown);
    }
    
    private System.Collections.IEnumerator PullSurvivor(GameObject survivor)
    {
        float distance = Vector3.Distance(transform.position, survivor.transform.position);
        float timeToPull = distance / hookPullSpeed;
        float elapsedTime = 0f;
        
        Vector3 startPosition = survivor.transform.position;
        
        while (elapsedTime < timeToPull)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / timeToPull;
            
            // Линейная интерполяция позиции выжившего к маньяку
            survivor.transform.position = Vector3.Lerp(startPosition, transform.position, t);
            
            yield return null;
        }
        
        // После притягивания можно нанести урон или оглушить
    }
    
    private void ResetHook()
    {
        if (IsServer)
        {
            networkHookReady.Value = true;
        }
    }
    
    private void PlayChainsawStart()
    {
        if (audioSource != null && chainsawStartSound != null)
        {
            audioSource.PlayOneShot(chainsawStartSound);
            Invoke(nameof(PlayChainsawLoop), chainsawStartSound.length);
        }
        
        if (chainsawSparks != null)
        {
            chainsawSparks.Play();
        }
    }
    
    private void PlayChainsawLoop()
    {
        if (audioSource != null && chainsawLoopSound != null)
        {
            audioSource.clip = chainsawLoopSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }
    
    private void StopChainsawLoop()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        
        if (chainsawSparks != null)
        {
            chainsawSparks.Stop();
        }
    }
    
    private void OnChainsawStateChanged(bool previous, bool current)
    {
        if (!IsOwner) return;
        
        if (current)
        {
            PlayChainsawStart();
        }
        else
        {
            StopChainsawLoop();
        }
    }
    
    /// <summary>
    /// Проверка, находится ли выживший в радиусе террора.
    /// Используется для звуковых эффектов и UI предупреждений.
    /// </summary>
    public bool IsSurvivorInTerrorRadius(Transform survivorTransform)
    {
        float distance = Vector3.Distance(transform.position, survivorTransform.position);
        return distance <= currentTerrorRadius;
    }
    
    public override void OnDestroy()
    {
        base.OnDestroy();
        networkChainsawActive.OnValueChanged -= OnChainsawStateChanged;
    }
}
