using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Объект эвакуации: Починка аварийного моста.
/// Требует ящик с инструментами, многоэтапный прогресс-бар.
/// </summary>
public class BridgeRepair : BaseEscapeObjective
{
    [Header("Bridge Settings")]
    [SerializeField] private GameObject bridgeModel; // Модель моста
    [SerializeField] private GameObject[] repairStages; // Этапы починки (визуальные)
    [SerializeField] private float totalRepairTime = 30f; // Общее время ремонта
    
    [Header("Multi-stage Repair")]
    [SerializeField] private int numberOfStages = 3; // Количество этапов ремонта
    [SerializeField] private float stageDuration = 10f; // Время каждого этапа
    
    [Header("Audio & Effects")]
    [SerializeField] private AudioClip hammerSound;
    [SerializeField] private AudioClip bridgeCreakSound;
    [SerializeField] private ParticleSystem repairSparks;
    
    private int currentStage = 0;
    private bool isBridgePassable = false;
    
    protected override void Awake()
    {
        base.Awake();
        
        objectiveName = "Аварийный мост";
        requiresItem = true;
        requiredItemType = ItemType.ToolBox;
        interactionTime = stageDuration;
        totalRepairTime = numberOfStages * stageDuration;
        
        // Скрытие этапов ремонта
        if (repairStages != null)
        {
            foreach (GameObject stage in repairStages)
            {
                if (stage != null)
                {
                    stage.SetActive(false);
                }
            }
        }
    }
    
    public override void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor)) return;
        
        currentInteractor = interactor;
        isInteracting = true;
        
        if (IsServer)
        {
            StartCoroutine(MultiStageRepairProcess());
        }
        
        if (startSound != null)
        {
            AudioSource.PlayClipAtPoint(startSound, transform.position);
        }
    }
    
    /// <summary>
    /// Многоэтапный процесс ремонта моста.
    /// Каждый этап требует отдельного взаимодействия.
    /// </summary>
    private System.Collections.IEnumerator MultiStageRepairProcess()
    {
        currentStage = 0;
        
        while (currentStage < numberOfStages)
        {
            currentProgress = 0f;
            
            // Выполнение текущего этапа
            while (currentProgress < 100f)
            {
                currentProgress += (100f / stageDuration) * Time.deltaTime;
                networkProgress.Value = ((currentStage + (currentProgress / 100f)) / numberOfStages) * 100f;
                
                // Проверка на прерывание
                if (currentInteractor == null || !currentInteractor.activeInHierarchy)
                {
                    InterruptInteraction();
                    yield break;
                }
                
                // Звуковые эффекты ремонта
                if (hammerSound != null && Random.value < 0.1f)
                {
                    AudioSource.PlayClipAtPoint(hammerSound, transform.position);
                }
                
                yield return null;
            }
            
            // Завершение этапа
            currentStage++;
            
            if (repairStages != null && currentStage <= repairStages.Length)
            {
                repairStages[currentStage - 1].SetActive(true);
            }
            
            // Короткая пауза между этапами
            yield return new WaitForSeconds(0.5f);
        }
        
        CompleteObjective();
    }
    
    protected override void CompleteObjective()
    {
        base.CompleteObjective();
        
        isBridgePassable = true;
        
        // Показываем восстановленный мост
        if (bridgeModel != null)
        {
            bridgeModel.SetActive(true);
        }
        
        // Воспроизведение звука готового моста
        if (bridgeCreakSound != null)
        {
            AudioSource.PlayClipAtPoint(bridgeCreakSound, transform.position);
        }
        
        if (repairSparks != null)
        {
            repairSparks.Stop();
        }
        
        // Уведомление о доступности пути эвакуации
        if (IsServer)
        {
            Debug.Log("Мост отремонтирован и готов к использованию!");
            GameManager.Instance?.OnSurvivorsEscaped(1); // 1 - ID пути эвакуации (мост)
        }
    }
    
    protected override void InterruptInteraction()
    {
        base.InterruptInteraction();
        
        // При прерывании можно сбросить текущий этап или сохранить прогресс
        // В данной реализации прогресс этапа сбрасывается
        if (repairSparks != null)
        {
            repairSparks.Stop();
        }
    }
    
    /// <summary>
    /// Использование моста выжившим.
    /// Вызывается когда выживший переходит по мосту.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void CrossBridgeServerRpc()
    {
        if (!IsServer || !isBridgePassable) return;
        
        // Логика перехода выжившего по мосту
        Debug.Log("Выживший переходит по мосту!");
        
        // Уведомление о победе (если это последний выживший)
        // GameManager.Instance?.OnSurvivorCrossedBridge(gameObject);
    }
    
    public override string GetInteractionPrompt()
    {
        if (IsCompleted)
        {
            return "Мост готов! Можно переходить.";
        }
        
        if (isInteracting)
        {
            return $"Ремонт... Этап {currentStage + 1}/{numberOfStages}";
        }
        
        if (requiresItem)
        {
            return $"Требуется: {requiredItemType} (Ящик с инструментами)";
        }
        
        return $"Ремонт моста ({totalRepairTime} сек)";
    }
    
    protected override void UpdateProgressUI(float progress)
    {
        base.UpdateProgressUI(progress);
        
        // Обновление UI прогресса ремонта
        // В реальной игре здесь нужно обновлять прогресс-бар в UI
        float overallProgress = ((currentStage + (progress / 100f)) / numberOfStages) * 100f;
        Debug.Log($"Прогресс ремонта: {overallProgress:F1}%");
    }
    
    protected override void OnObjectiveCompleted()
    {
        base.OnObjectiveCompleted();
        
        // Визуальные эффекты на клиенте
        if (bridgeModel != null)
        {
            bridgeModel.SetActive(true);
        }
    }
}
