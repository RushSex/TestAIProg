using UnityEngine;

/// <summary>
/// Обработчик ввода для PC (клавиатура + мышь).
/// Схема управления: WASD + Mouse Look + клавиши действий.
/// </summary>
public class PCInputHandler : PlayerInputHandler
{
    [Header("PC Key Bindings")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode skillKey = KeyCode.Q;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;

    private float verticalInput;
    private float horizontalInput;
    private float mouseX;
    private float mouseY;
    private bool interactButtonDown;
    private bool skillButtonDown;

    public override Vector2 GetMovementInput()
    {
        verticalInput = Input.GetAxis("Vertical"); // W/S
        horizontalInput = Input.GetAxis("Horizontal"); // A/D

        return new Vector2(horizontalInput, verticalInput).normalized;
    }

    public override Vector2 GetLookInput()
    {
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");

        return new Vector2(mouseX * lookSensitivity, mouseY * lookSensitivity);
    }

    public override bool IsSprintPressed()
    {
        return Input.GetKey(sprintKey);
    }
    
    /// <summary>
    /// Проверяет, зажата ли кнопка бега.
    /// </summary>
    public bool GetRunButton()
    {
        return Input.GetKey(sprintKey);
    }

    public override bool IsInteractPressed()
    {
        return Input.GetKey(interactKey);
    }
    
    /// <summary>
    /// Проверяет нажатие кнопки взаимодействия (только один кадр).
    /// </summary>
    public bool GetInteractButtonDown()
    {
        return Input.GetKeyDown(interactKey);
    }

    public override bool IsSkillPressed()
    {
        return Input.GetKey(skillKey);
    }
    
    /// <summary>
    /// Проверяет нажатие кнопки навыка (только один кадр).
    /// </summary>
    public bool GetSkillButtonDown()
    {
        return Input.GetKeyDown(skillKey);
    }

    public override bool IsJumpPressed()
    {
        return Input.GetKeyDown(jumpKey);
    }

    public override void UpdateInput()
    {
        base.UpdateInput();
        jumpPressed = IsJumpPressed();
        interactButtonDown = GetInteractButtonDown();
        skillButtonDown = GetSkillButtonDown();
    }
}
