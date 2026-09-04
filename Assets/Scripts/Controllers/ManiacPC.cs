using UnityEngine;

/// <summary>
/// Контроллер маньяка для PC платформы.
/// Управление: WASD + Mouse, бензопила (ЛКМ), крюк (ПКМ).
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PCInputHandler))]
public class ManiacPC : BaseCharacter
{
    [Header("Maniac PC Settings")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float minVerticalAngle = -45f;
    [SerializeField] private float maxVerticalAngle = 80f;
    
    private PCInputHandler pcInput;
    private float verticalRotation = 0f;
    private bool isChainsawActive = false;
    
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
        
        Vector2 moveInput = pcInput.GetMovementInput();
        Vector2 lookInput = pcInput.GetLookInput();
        
        verticalRotation -= lookInput.y;
        verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);
        
        transform.Rotate(Vector3.up, lookInput.x);
        
        if (cameraPivot != null)
        {
            cameraPivot.localEulerAngles = new Vector3(verticalRotation, 0f, 0f);
        }
        
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
        
        // Атака бензопилой (ЛКМ)
        if (pcInput.GetInteractButtonDown())
        {
            ChainsawAttackServerRpc(OwnerClientId);
        }
        
        // Крюк-гарпун (ПКМ)
        if (pcInput.GetSkillButtonDown())
        {
            HookAttackServerRpc(OwnerClientId);
        }
    }
    
    protected override void HandleMovement()
    {
        if (!IsOwner || IsDead) return;
        
        float currentSpeed = baseMoveSpeed;
        
        if (isChainsawActive)
            currentSpeed *= 1.3f; // Ускорение при включенной бензопиле
        
        if (moveDirection != Vector3.zero)
        {
            Vector3 targetVelocity = moveDirection * currentSpeed;
            targetVelocity.y = characterController.velocity.y;
            
            characterController.Move(targetVelocity * Time.deltaTime);
        }
    }
    
    protected override bool IsRunning()
    {
        return moveDirection.magnitude > 0.1f && isChainsawActive;
    }
    
    public override void TakeDamage(float damage, DamageSource source)
    {
        // Маньяк не получает урон от выживших
        if (!IsServer) return;
    }
    
    /// <summary>
    /// Атака бензопилой.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void ChainsawAttackServerRpc(ulong attackerId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        
        var maniacController = GetComponent<ManiacController>();
        if (maniacController != null)
        {
            maniacController.PerformChainsawAttack();
        }
    }
    
    /// <summary>
    /// Атака крюком-гарпуном.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void HookAttackServerRpc(ulong attackerId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        
        var maniacController = GetComponent<ManiacController>();
        if (maniacController != null)
        {
            maniacController.PerformHookAttack(transform.position, transform.forward);
        }
    }
}
