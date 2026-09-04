using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Обработчик ввода для мобильных устройств (сенсорный экран).
/// Схема управления: Виртуальный джойстик + свайпы камеры + UI кнопки.
/// </summary>
public class MobileInputHandler : PlayerInputHandler
{
    [Header("Mobile UI References")]
    [SerializeField] private RectTransform joystickArea;
    [SerializeField] private RectTransform cameraArea;
    [SerializeField] private UnityEngine.UI.Button sprintButton;
    [SerializeField] private UnityEngine.UI.Button interactButton;
    [SerializeField] private UnityEngine.UI.Button skillButton;
    
    [Header("Joystick Settings")]
    [SerializeField] private float joystickRadius = 50f;
    [SerializeField] private float deadZone = 0.1f;
    
    [Header("Camera Settings")]
    [SerializeField] private float cameraSensitivity = 0.5f;
    
    // Состояния джойстика
    private int joystickTouchId = -1;
    private Vector2 joystickCenter;
    private Vector2 joystickCurrent;
    private Vector2 joystickDelta;
    
    // Состояния камеры
    private int cameraTouchId = -1;
    private Vector2 lastCameraPosition;
    private Vector2 cameraDelta;
    
    // Состояния кнопок
    private bool sprintButtonDown;
    private bool interactButtonDown;
    private bool skillButtonDown;
    
    private void Start()
    {
        SetupButtonListeners();
    }
    
    private void SetupButtonListeners()
    {
        if (sprintButton != null)
            sprintButton.onClick.AddListener(() => sprintButtonDown = true);
        
        if (interactButton != null)
            interactButton.onClick.AddListener(() => interactButtonDown = true);
        
        if (skillButton != null)
            skillButton.onClick.AddListener(() => skillButtonDown = true);
    }
    
    public override Vector2 GetMovementInput()
    {
        if (joystickTouchId == -1)
            return Vector2.zero;
        
        joystickDelta = joystickCurrent - joystickCenter;
        
        // Нормализация и ограничение радиуса
        if (joystickDelta.magnitude > joystickRadius)
            joystickDelta = joystickDelta.normalized * joystickRadius;
        
        // Преобразование в нормализованный вектор [-1, 1]
        Vector2 input = joystickDelta / joystickRadius;
        
        // Применение мертвой зоны
        if (input.magnitude < deadZone)
            return Vector2.zero;
        
        return input.normalized;
    }
    
    public override Vector2 GetLookInput()
    {
        if (cameraTouchId == -1)
            return Vector2.zero;
        
        Vector2 result = cameraDelta * cameraSensitivity;
        cameraDelta = Vector2.zero; // Сброс после чтения
        
        return result;
    }
    
    public override bool IsSprintPressed()
    {
        bool result = sprintButtonDown;
        sprintButtonDown = false; // Сброс после чтения
        return result;
    }
    
    public override bool IsInteractPressed()
    {
        bool result = interactButtonDown;
        interactButtonDown = false;
        return result;
    }
    
    public override bool IsSkillPressed()
    {
        bool result = skillButtonDown;
        skillButtonDown = false;
        return result;
    }
    
    public override bool IsJumpPressed()
    {
        // Прыжок не используется в этой игре
        return false;
    }
    
    private void Update()
    {
        ProcessTouchInputs();
        base.UpdateInput();
    }
    
    private void ProcessTouchInputs()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            Vector2 touchPos = touch.position;
            
            // Проверка попадания в зону джойстика (левая половина экрана)
            if (IsInJoystickArea(touchPos))
            {
                if (touch.phase == TouchPhase.Began && joystickTouchId == -1)
                {
                    joystickTouchId = touch.fingerId;
                    joystickCenter = touchPos;
                    joystickCurrent = touchPos;
                }
                else if (touch.fingerId == joystickTouchId)
                {
                    switch (touch.phase)
                    {
                        case TouchPhase.Moved:
                        case TouchPhase.Stationary:
                            joystickCurrent = touchPos;
                            break;
                        case TouchPhase.Ended:
                        case TouchPhase.Canceled:
                            joystickTouchId = -1;
                            joystickDelta = Vector2.zero;
                            break;
                    }
                }
            }
            // Проверка попадания в зону камеры (правая половина экрана)
            else if (IsInCameraArea(touchPos))
            {
                if (touch.phase == TouchPhase.Began && cameraTouchId == -1)
                {
                    cameraTouchId = touch.fingerId;
                    lastCameraPosition = touchPos;
                }
                else if (touch.fingerId == cameraTouchId)
                {
                    switch (touch.phase)
                    {
                        case TouchPhase.Moved:
                            Vector2 delta = touchPos - lastCameraPosition;
                            cameraDelta += delta;
                            lastCameraPosition = touchPos;
                            break;
                        case TouchPhase.Ended:
                        case TouchPhase.Canceled:
                            cameraTouchId = -1;
                            cameraDelta = Vector2.zero;
                            break;
                    }
                }
            }
        }
    }
    
    private bool IsInJoystickArea(Vector2 position)
    {
        // Левая половина экрана или специальная зона UI
        if (joystickArea != null)
        {
            RectTransformUtility.RectangleContainsScreenPoint(joystickArea, position, null);
        }
        return position.x < Screen.width / 2f;
    }
    
    private bool IsInCameraArea(Vector2 position)
    {
        // Правая половина экрана или специальная зона UI
        if (cameraArea != null)
        {
            RectTransformUtility.RectangleContainsScreenPoint(cameraArea, position, null);
        }
        return position.x >= Screen.width / 2f;
    }
    
    /// <summary>
    /// Вызывается UI кнопками при нажатии.
    /// </summary>
    public void OnSprintButtonPressed() => sprintButtonDown = true;
    public void OnInteractButtonPressed() => interactButtonDown = true;
    public void OnSkillButtonPressed() => skillButtonDown = true;
}
