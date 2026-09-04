using UnityEngine;

/// <summary>
/// Контроллер маньяка для мобильных платформ (Android/iOS).
/// Экранное управление: джойстик + кнопки атаки.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(MobileInputHandler))]
public class ManiacMobile : BaseCharacter
{
    [Header("Maniac Mobile Settings")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float minVerticalAngle = -45f;
    [SerializeField] private float maxVerticalAngle = 80f;
    
    private MobileInputHandler mobileInput;
    private float verticalRotation = 0f;
    private bool isChainsawActive = false;
    
    protected override void Awake()
    {
        base.Awake();
        mobileInput = GetComponent<MobileInputHandler>();
    }
    
    protected override void InitializeInput()
    {
        if (mobileInput == null)
            mobileInput = GetComponent<MobileInputHandler>();
    }
    
    protected override void HandleInput()
    {
        if (!IsOwner || IsDead) return;
        
        mobileInput.UpdateInput();
        
        Vector2 moveInput = mobileInput.GetMovementInput();
        Vector2 lookInput = mobileInput.GetLookInput();
        
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
        
        // Атака бензопилой (кнопка на экране)
        if (mobileInput.GetInteractButtonDown())
        {
            ChainsawAttackServerRpc(OwnerClientId);
        }
        
        // Крюк-гарпун (кнопка на экране)
        if (mobileInput.GetSkillButtonDown())
        {
            HookAttackServerRpc(OwnerClientId);
        }
    }
    
    protected override void HandleMovement()
    {
        if (!IsOwner || IsDead) return;
        
        float currentSpeed = baseMoveSpeed;
        
        if (isChainsawActive)
            currentSpeed *= 1.3f;
        
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
