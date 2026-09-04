using UnityEngine;
using System;

/// <summary>
/// Абстрактный базовый класс для ввода игрока.
/// Реализует паттерн Strategy для поддержки PC и Mobile управления.
/// </summary>
public abstract class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] protected float moveSpeed = 5f;
    [SerializeField] protected float lookSensitivity = 2f;
    [SerializeField] protected float sprintMultiplier = 1.6f;
    
    // Вектор движения (X - стрейф, Y - вперед/назад)
    protected Vector2 moveInput;
    
    // Вращение камеры (X - горизонталь, Y - вертикаль)
    protected Vector2 lookInput;
    
    // Состояния кнопок
    protected bool sprintPressed;
    protected bool interactPressed;
    protected bool skillPressed;
    protected bool jumpPressed;
    
    /// <summary>
    /// Получить вектор движения от системы ввода.
    /// </summary>
    /// <returns>Нормализованный вектор 2D (forward, strafe)</returns>
    public abstract Vector2 GetMovementInput();
    
    /// <summary>
    /// Получить вращение камеры.
    /// </summary>
    /// <returns>Вектор 2D (horizontal, vertical)</returns>
    public abstract Vector2 GetLookInput();
    
    /// <summary>
    /// Проверка нажатия кнопки спринта.
    /// </summary>
    public abstract bool IsSprintPressed();
    
    /// <summary>
    /// Проверка нажатия кнопки взаимодействия.
    /// </summary>
    public abstract bool IsInteractPressed();
    
    /// <summary>
    /// Проверка нажатия кнопки навыка/способности.
    /// </summary>
    public abstract bool IsSkillPressed();
    
    /// <summary>
    /// Проверка нажатия кнопки прыжка (если применимо).
    /// </summary>
    public abstract bool IsJumpPressed();
    
    /// <summary>
    /// Получить состояние кнопки взаимодействия (just pressed).
    /// </summary>
    public bool GetInteractButtonDown()
    {
        bool current = IsInteractPressed();
        bool result = current && !previousInteractState;
        previousInteractState = current;
        return result;
    }
    
    /// <summary>
    /// Получить состояние кнопки навыка (just pressed).
    /// </summary>
    public bool GetSkillButtonDown()
    {
        bool current = IsSkillPressed();
        bool result = current && !previousSkillState;
        previousSkillState = current;
        return result;
    }
    
    private bool previousInteractState;
    private bool previousSkillState;
    
    /// <summary>
    /// Обновить внутренние значения ввода.
    /// Вызывается из Update().
    /// </summary>
    public virtual void UpdateInput()
    {
        moveInput = GetMovementInput();
        lookInput = GetLookInput();
        sprintPressed = IsSprintPressed();
        
        // Кэшируем состояния для detection just pressed
        bool currentInteract = IsInteractPressed();
        bool currentSkill = IsSkillPressed();
        
        interactPressed = GetInteractButtonDown();
        skillPressed = GetSkillButtonDown();
    }
    
    /// <summary>
    /// Получить текущую скорость движения с учетом спринта.
    /// </summary>
    public float GetCurrentMoveSpeed()
    {
        return sprintPressed ? moveSpeed * sprintMultiplier : moveSpeed;
    }
}
