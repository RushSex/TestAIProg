using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Объект эвакуации: Побег на катере.
/// Требует ключ зажигания, запускает таймер двигателя.
/// </summary>
public class BoatEscape : BaseEscapeObjective
{
    [Header("Boat Settings")]
    [SerializeField] private GameObject boatModel;
    [SerializeField] private Transform smokeEmitter;
    [SerializeField] private ParticleSystem engineSmoke;
    
    [Header("Audio")]
    [SerializeField] private AudioClip engineStartSound;
    [SerializeField] private AudioClip engineLoopSound;
    
    private AudioSource boatAudioSource;
    
    protected override void Awake()
    {
        base.Awake();
        
        objectiveName = "Катер";
        requiresItem = true;
        requiredItemType = ItemType.IgnitionKey;
        interactionTime = 15f; // Время запуска двигателя
        
        boatAudioSource = GetComponent<AudioSource>();
    }
    
    protected override void CompleteObjective()
    {
        base.CompleteObjective();
        
        // Запуск эффектов катера
        if (engineSmoke != null)
        {
            engineSmoke.Play();
        }
        
        if (boatAudioSource != null && engineLoopSound != null)
        {
            boatAudioSource.clip = engineLoopSound;
            boatAudioSource.loop = true;
            boatAudioSource.Play();
        }
        
        // Уведомление всех игроков о победе выживших
        if (IsServer)
        {
            GameManager.Instance?.OnSurvivorsEscaped(3); // 3 - ID пути эвакуации (катер)
        }
    }
    
    public override string GetInteractionPrompt()
    {
        if (IsCompleted)
        {
            return "Катер готов!";
        }
        
        if (requiresItem)
        {
            SurvivorHealth health = GetComponent<SurvivorHealth>();
            if (health == null || !health.CanHelpOthers())
            {
                return "Нельзя взаимодействовать";
            }
            
            // Проверка инвентаря через raycast при наведении
            return $"Вставить ключ зажигания ({interactionTime} сек)";
        }
        
        return base.GetInteractionPrompt();
    }
    
    protected override void OnObjectiveCompleted()
    {
        base.OnObjectiveCompleted();
        
        // Визуальные эффекты на клиенте
        if (engineSmoke != null)
        {
            engineSmoke.Play();
        }
    }
}
