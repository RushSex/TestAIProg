using UnityEngine;

/// <summary>
/// Контроллер выжившего для PC платформы.
/// Использует клавиатуру и мышь для управления.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PCInputHandler))]
public class SurvivorPC : BaseCharacter
{
    [Header("PC Camera Settings")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float minVerticalAngle = -45f;
    [SerializeField] private float maxVerticalAngle = 80f;
    
    private PCInputHandler pcInput;
    private float verticalRotation = 0f;
    private bool isRunningPressed = false;
    
    protected override void Awake()
    {
        base.Awake();
        pcInput = GetComponent<PCInputHandler>();
    }
    
    protected override void InitializeInput()
    {
        if (pcInput == null)
            pcInput = GetComponent<PCInputHandler>();
    }
    
    protected override void HandleInput()
    {
        if (!IsOwner || IsDead) return;
        
        pcInput.UpdateInput();
        
        // Получение ввода движения
        Vector2 moveInput = pcInput.GetMovementInput();
        Vector2 lookInput = pcInput.GetLookInput();
        
        // Вращение камеры по вертикали (мышь Y)
        verticalRotation -= lookInput.y;
        verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);
        
        // Вращение персонажа по горизонтали (мышь X)
        transform.Rotate(Vector3.up, lookInput.x);
        
        // Применение вращения камеры
        if (cameraPivot != null)
        {
            cameraPivot.localEulerAngles = new Vector3(verticalRotation, 0f, 0f);
        }
        
        // Вычисление направления движения относительно камеры
        if (moveInput != Vector2.zero)
        {
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            
            moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
        }
        else
        {
            moveDirection = Vector3.zero;
        }
        
        // Проверка бега
        isRunningPressed = pcInput.GetRunButton();
        
        // Проверка взаимодействия
        if (pcInput.GetInteractButtonDown())
        {
            TryInteractServerRpc(OwnerClientId);
        }
        
        // Проверка использования навыка
        if (pcInput.GetSkillButtonDown())
        {
            UseSkillServerRpc(OwnerClientId);
        }
    }
    
    protected override void HandleMovement()
    {
        if (!IsOwner || IsDead) return;
        
        float currentSpeed = baseMoveSpeed;
        
        if (isRunningPressed)
            currentSpeed *= runSpeedMultiplier;
        
        // Учет состояния здоровья (если есть компонент SurvivorHealth)
        var health = GetComponent<SurvivorHealth>();
        if (health != null)
        {
            var state = health.GetHealthState();
            if (state == SurvivorHealthState.Injured)
                currentSpeed *= 0.75f;
            else if (state == SurvivorHealthState.Downed)
                currentSpeed *= 0.3f;
        }
        
        if (moveDirection != Vector3.zero)
        {
            Vector3 targetVelocity = moveDirection * currentSpeed;
            targetVelocity.y = characterController.velocity.y; // Сохраняем вертикальную скорость (гравитация)
            
            characterController.Move(targetVelocity * Time.deltaTime);
        }
    }
    
    protected override bool IsRunning()
    {
        return isRunningPressed && moveDirection.magnitude > 0.1f;
    }
    
    public override void TakeDamage(float damage, DamageSource source)
    {
        if (!IsServer) return;
        
        var health = GetComponent<SurvivorHealth>();
        if (health != null)
        {
            health.TakeDamage(damage, source);
        }
    }
    
    /// <summary>
    /// Попытка взаимодействия с объектом (вызывается при нажатии E).
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void TryInteractServerRpc(ulong interactorId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        
        // Рейкаст вперед от игрока
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f;
        if (Physics.Raycast(rayOrigin, transform.forward, out hit, 3f))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                // Проверка дистанции
                float distance = Vector3.Distance(transform.position, hit.point);
                if (distance <= interactable.GetInteractionDistance())
                {
                    interactable.Interact(OwnerClientId);
                }
            }
        }
    }
    
    /// <summary>
    /// Использование навыка/способности (вызывается при нажатии Q).
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void UseSkillServerRpc(ulong playerId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        
        // Для выжившего это может быть использование предмета из инвентаря
        // Реализуется в классе SurvivorInventory
        var inventory = GetComponent<SurvivorInventory>();
        if (inventory != null)
        {
            inventory.UseActiveItemServerRpc(OwnerClientId);
        }
    }
}
