using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Базовый класс для всех интерактивных объектов эвакуации.
/// Реализует общую логику: прогресс взаимодействия, сетевая синхронизация.
/// </summary>
public abstract class BaseEscapeObjective : NetworkBehaviour, IInteractable
{
    [Header("Objective Settings")]
    [SerializeField] protected float interactionTime = 10f; // Время выполнения взаимодействия
    [SerializeField] protected string objectiveName = "Escape Objective";
    
    [Header("Requirements")]
    [SerializeField] protected bool requiresItem = false;
    [SerializeField] protected ItemType requiredItemType;
    
    [Header("Audio & Effects")]
    [SerializeField] protected AudioClip startSound;
    [SerializeField] protected AudioClip completeSound;
    [SerializeField] protected GameObject completionEffect;
    
    protected bool isCompleted = false;
    protected bool isInteracting = false;
    protected float currentProgress = 0f;
    protected GameObject currentInteractor;
    
    // NetworkVariables
    protected NetworkVariable<bool> networkIsCompleted = new NetworkVariable<bool>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );
    
    protected NetworkVariable<float> networkProgress = new NetworkVariable<float>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );
    
    public bool IsCompleted => networkIsCompleted.Value;
    public string ObjectiveName => objectiveName;
    
    protected virtual void Start()
    {
        networkIsCompleted.OnValueChanged += OnCompletionStateChanged;
        networkProgress.OnValueChanged += OnProgressChanged;
    }
    
    /// <summary>
    /// Проверка возможности взаимодействия с объектом.
    /// </summary>
    public virtual bool CanInteract(GameObject interactor)
    {
        if (isCompleted || isInteracting) return false;
        
        SurvivorHealth survivorHealth = interactor.GetComponent<SurvivorHealth>();
        if (survivorHealth == null || !survivorHealth.CanHelpOthers())
        {
            return false;
        }
        
        if (requiresItem)
        {
            SurvivorInventory inventory = interactor.GetComponent<SurvivorInventory>();
            if (inventory == null || !inventory.HasItem(requiredItemType))
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Начало взаимодействия с объектом.
    /// </summary>
    public virtual void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor)) return;
        
        currentInteractor = interactor;
        isInteracting = true;
        
        if (IsServer)
        {
            StartCoroutine(InteractionProcess());
        }
        
        if (startSound != null)
        {
            AudioSource.PlayClipAtPoint(startSound, transform.position);
        }
    }
    
    /// <summary>
    /// Процесс взаимодействия с прогресс-баром.
    /// Может быть прерван атакой маньяка или другими событиями.
    /// </summary>
    protected virtual System.Collections.IEnumerator InteractionProcess()
    {
        currentProgress = 0f;
        
        while (currentProgress < 100f)
        {
            currentProgress += (100f / interactionTime) * Time.deltaTime;
            networkProgress.Value = currentProgress;
            
            // Проверка на прерывание (например, если интерактор умер или ушел)
            if (currentInteractor == null || !currentInteractor.activeInHierarchy)
            {
                InterruptInteraction();
                yield break;
            }
            
            yield return null;
        }
        
        CompleteObjective();
    }
    
    /// <summary>
    /// Прерывание взаимодействия.
    /// </summary>
    protected virtual void InterruptInteraction()
    {
        isInteracting = false;
        currentProgress = 0f;
        networkProgress.Value = 0f;
        currentInteractor = null;
    }
    
    /// <summary>
    /// Завершение объекта эвакуации.
    /// Вызывается когда прогресс достиг 100%.
    /// </summary>
    protected virtual void CompleteObjective()
    {
        isCompleted = true;
        isInteracting = false;
        networkIsCompleted.Value = true;
        
        if (completeSound != null)
        {
            AudioSource.PlayClipAtPoint(completeSound, transform.position);
        }
        
        if (completionEffect != null)
        {
            Instantiate(completionEffect, transform.position, Quaternion.identity);
        }
        
        // Уведомление GameManager о завершении
        GameManager.Instance?.OnObjectiveCompleted(this);
        
        // Удаление предмета из инвентаря если требуется
        if (requiresItem && currentInteractor != null)
        {
            SurvivorInventory inventory = currentInteractor.GetComponent<SurvivorInventory>();
            inventory?.RemoveItem(requiredItemType);
        }
        
        currentInteractor = null;
    }
    
    /// <summary>
    /// Возвращает подсказку о необходимом действии.
    /// </summary>
    public virtual string GetInteractionPrompt()
    {
        if (isCompleted)
        {
            return "Завершено";
        }
        
        if (requiresItem)
        {
            return $"Требуется: {requiredItemType}";
        }
        
        return $"Взаимодействовать ({interactionTime} сек)";
    }
    
    private void OnCompletionStateChanged(bool previous, bool current)
    {
        if (current)
        {
            OnObjectiveCompleted();
        }
    }
    
    private void OnProgressChanged(float previous, float current)
    {
        UpdateProgressUI(current);
    }
    
    /// <summary>
    /// Обновление UI прогресса (переопределяется в наследниках).
    /// </summary>
    protected virtual void UpdateProgressUI(float progress)
    {
        // Реализация зависит от конкретной системы UI
    }
    
    /// <summary>
    /// Вызывается при завершении объекта (на клиентах).
    /// </summary>
    protected virtual void OnObjectiveCompleted()
    {
        // Визуальные эффекты, звуки и т.д.
    }
    
    public override void OnDestroy()
    {
        base.OnDestroy();
        networkIsCompleted.OnValueChanged -= OnCompletionStateChanged;
        networkProgress.OnValueChanged -= OnProgressChanged;
    }
}
