using UnityEngine;
using Unity.Netcode;
using System;

/// <summary>
/// Базовый класс для всех персонажей в игре (Маньяк и Выжившие).
/// Реализует общую логику: перемещение, состояния, сетевая синхронизация.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkObject))]
public abstract class BaseCharacter : NetworkBehaviour
{
    [Header("Base Stats")]
    [SerializeField] protected float baseMoveSpeed = 5f;
    [SerializeField] protected float runSpeedMultiplier = 1.6f;
    [SerializeField] protected float rotationSpeed = 10f;
    
    [Header("State")]
    protected CharacterState currentState = CharacterState.Idle;
    protected CharacterController characterController;
    protected Vector3 moveDirection;
    protected bool isGrounded;
    
    // NetworkVariables для синхронизации состояний
    protected NetworkVariable<CharacterState> networkState = new NetworkVariable<CharacterState>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );
    
    protected NetworkVariable<Vector3> networkVelocity = new NetworkVariable<Vector3>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );

    protected virtual void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    protected virtual void Start()
    {
        if (!IsOwner) return;
        
        // Инициализация управления только для локального игрока
        InitializeInput();
    }

    protected virtual void Update()
    {
        if (!IsOwner) return;
        
        HandleInput();
        UpdateState();
    }

    /// <summary>
    /// Инициализация системы ввода (вызывается один раз при старте).
    /// Переопределяется в наследниках для PC/Mobile специфики.
    /// </summary>
    protected abstract void InitializeInput();

    /// <summary>
    /// Обработка ввода от игрока.
    /// </summary>
    protected abstract void HandleInput();

    /// <summary>
    /// Обновление состояния персонажа на основе текущего ввода и условий.
    /// </summary>
    protected virtual void UpdateState()
    {
        // Базовая логика переключения состояний
        switch (currentState)
        {
            case CharacterState.Idle:
                if (moveDirection.magnitude > 0.1f)
                {
                    ChangeState(CharacterState.Moving);
                }
                break;
                
            case CharacterState.Moving:
                if (moveDirection.magnitude < 0.1f)
                {
                    ChangeState(CharacterState.Idle);
                }
                else if (IsRunning())
                {
                    ChangeState(CharacterState.Running);
                }
                break;
                
            case CharacterState.Running:
                if (!IsRunning() || moveDirection.magnitude < 0.1f)
                {
                    ChangeState(CharacterState.Moving);
                }
                break;
        }
        
        MoveCharacter();
    }

    /// <summary>
    /// Физическое перемещение персонажа.
    /// </summary>
    protected virtual void MoveCharacter()
    {
        if (currentState == CharacterState.Hooked || 
            currentState == CharacterState.Interacting ||
            currentState == CharacterState.Repairing)
        {
            return;
        }

        float speed = GetMovementSpeed();
        Vector3 movement = moveDirection * speed * Time.deltaTime;
        
        characterController.Move(movement);
        
        // Поворот персонажа в направлении движения
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        // Синхронизация скорости по сети
        if (IsServer)
        {
            networkVelocity.Value = moveDirection * speed;
        }
    }

    /// <summary>
    /// Возвращает текущую скорость перемещения с учетом состояния.
    /// </summary>
    protected virtual float GetMovementSpeed()
    {
        float speed = baseMoveSpeed;
        
        if (currentState == CharacterState.Running)
        {
            speed *= runSpeedMultiplier;
        }
        
        return speed;
    }

    /// <summary>
    /// Проверяет, бежит ли персонаж в данный момент.
    /// </summary>
    protected abstract bool IsRunning();

    /// <summary>
    /// Изменение состояния персонажа с сетевой синхронизацией.
    /// </summary>
    protected void ChangeState(CharacterState newState)
    {
        if (currentState == newState) return;
        
        currentState = newState;
        
        if (IsServer)
        {
            networkState.Value = newState;
        }
    }

    /// <summary>
    /// Взаимодействие с интерактивным объектом.
    /// </summary>
    public virtual void TryInteract()
    {
        if (currentState == CharacterState.Interacting || 
            currentState == CharacterState.Repairing)
        {
            return;
        }

        // Raycast для поиска интерактивных объектов перед персонажем
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f; // Уровень глаз
        
        if (Physics.Raycast(rayOrigin, transform.forward, out hit, 2.5f))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            
            if (interactable != null && interactable.CanInteract(gameObject))
            {
                ChangeState(CharacterState.Interacting);
                interactable.Interact(gameObject);
                
                // Возврат в предыдущее состояние после взаимодействия
                Invoke(nameof(ResetInteractionState), 0.5f);
            }
        }
    }

    private void ResetInteractionState()
    {
        if (currentState == CharacterState.Interacting)
        {
            ChangeState(CharacterState.Idle);
        }
    }

    /// <summary>
    /// Получение урона (реализуется в наследниках).
    /// </summary>
    public abstract void TakeDamage(float damage, DamageSource source);

    /// <summary>
    /// Тип источника урона.
    /// </summary>
    public enum DamageSource
    {
        Chainsaw,   // Бензопила маньяка
        Hook,       // Крюк маньяка
        Fall,       // Падение
        Other       // Другой источник
    }
}
