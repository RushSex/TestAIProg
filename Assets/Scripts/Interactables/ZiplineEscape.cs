using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Объект эвакуации: Спуск по канату (Зиплайн).
/// Требует бухту троса, включает QTE-проверку при закреплении.
/// </summary>
public class ZiplineEscape : BaseEscapeObjective
{
    [Header("Zipline Settings")]
    [SerializeField] private Transform startPoint; // Точка крепления на утесе
    [SerializeField] private Transform endPoint;   // Точка в безопасной зоне
    [SerializeField] private LineRenderer ziplineLine;
    [SerializeField] private GameObject ziplinePrefab; // Префаб для спуска
    
    [Header("QTE Settings")]
    [SerializeField] private float qteDuration = 5f; // Время на прохождение QTE
    [SerializeField] private float qteSuccessZone = 0.2f; // Размер успешной зоны (0-1)
    
    [Header("Audio & Effects")]
    [SerializeField] private AudioClip ropeAttachSound;
    [SerializeField] private AudioClip ziplineWhooshSound;
    
    private bool isZiplineReady = false;
    private bool isQTEActive = false;
    
    protected override void Awake()
    {
        base.Awake();
        
        objectiveName = "Зиплайн";
        requiresItem = true;
        requiredItemType = ItemType.RopeCoil;
        interactionTime = qteDuration; // Используем время QTE как время взаимодействия
        
        if (ziplineLine != null)
        {
            ziplineLine.enabled = false;
        }
    }
    
    public override void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor)) return;
        
        currentInteractor = interactor;
        isInteracting = true;
        isQTEActive = true;
        
        // Запуск QTE вместо обычного таймера
        if (IsServer)
        {
            StartCoroutine(QTEProcess());
        }
        
        if (startSound != null)
        {
            AudioSource.PlayClipAtPoint(startSound, transform.position);
        }
    }
    
    /// <summary>
    /// Процесс QTE проверки вместо стандартного таймера.
    /// </summary>
    private System.Collections.IEnumerator QTEProcess()
    {
        currentProgress = 0f;
        float qteTimer = 0f;
        bool qteSuccess = false;
        
        // Игрок должен нажать кнопку в правильный момент
        while (qteTimer < qteDuration)
        {
            qteTimer += Time.deltaTime;
            currentProgress = (qteTimer / qteDuration) * 100f;
            networkProgress.Value = currentProgress;
            
            // Проверка нажатия кнопки взаимодействия (упрощенно)
            // В реальной игре нужно отслеживать ввод от текущего интерактора
            if (Input.GetKeyDown(KeyCode.E) && !qteSuccess)
            {
                float normalizedTime = qteTimer / qteDuration;
                
                // Проверка попадания в успешную зону (например, 80-100% таймера)
                if (normalizedTime >= (1f - qteSuccessZone))
                {
                    qteSuccess = true;
                    break;
                }
            }
            
            yield return null;
        }
        
        if (qteSuccess)
        {
            CompleteObjective();
        }
        else
        {
            InterruptInteraction();
            // Можно добавить штраф за провал QTE
        }
    }
    
    protected override void CompleteObjective()
    {
        base.CompleteObjective();
        
        isZiplineReady = true;
        
        // Показываем линию зиплайна
        if (ziplineLine != null)
        {
            ziplineLine.SetPosition(0, startPoint.position);
            ziplineLine.SetPosition(1, endPoint.position);
            ziplineLine.enabled = true;
        }
        
        // Воспроизведение звука закрепления троса
        if (ropeAttachSound != null)
        {
            AudioSource.PlayClipAtPoint(ropeAttachSound, transform.position);
        }
        
        // Создание префаба зиплайна для визуализации
        if (ziplinePrefab != null)
        {
            GameObject zipline = Instantiate(ziplinePrefab, startPoint.position, Quaternion.identity);
            // Настройка зиплайна...
        }
        
        // Уведомление о доступности пути эвакуации
        if (IsServer)
        {
            Debug.Log("Зиплайн готов к использованию!");
            // GameManager.Instance?.OnEscapePathAvailable(2); // 2 - ID зиплайна
        }
    }
    
    /// <summary>
    /// Использование зиплайна выжившим.
    /// Вызывается когда выживший начинает спуск.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void UseZiplineServerRpc()
    {
        if (!IsServer || !isZiplineReady) return;
        
        // Логика спуска выжившего
        // В реальной игре нужно переместить выжившего в безопасную зону
        Debug.Log("Выживший использует зиплайн!");
        
        // Уведомление о победе
        GameManager.Instance?.OnSurvivorsEscaped(2); // 2 - ID пути эвакуации (зиплайн)
    }
    
    public override string GetInteractionPrompt()
    {
        if (IsCompleted)
        {
            return "Зиплайн готов! Нажмите для спуска.";
        }
        
        if (isQTEActive)
        {
            return "Нажмите E в правильной момент!";
        }
        
        if (requiresItem)
        {
            return $"Требуется: {requiredItemType} (Бухта троса)";
        }
        
        return base.GetInteractionPrompt();
    }
    
    protected override void OnObjectiveCompleted()
    {
        base.OnObjectiveCompleted();
        
        // Визуальные эффекты на клиенте
        if (ziplineLine != null)
        {
            ziplineLine.enabled = true;
        }
    }
}
