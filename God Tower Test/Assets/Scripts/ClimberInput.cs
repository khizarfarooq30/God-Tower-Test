using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ClimberInput : MonoBehaviour
{
    [SerializeField] private TowerClimber climber;

    [Header("Hold To Climb")]
    [SerializeField, Min(0f)] private float climbRepeatDelay = 0.05f;

    [Header("Swipe")]
    [SerializeField, Min(0f)] private float degreesPerScreenWidth = 180f;
    [SerializeField, Min(0f)] private float swipeDeadZonePixels = 5f;

    private bool pointerHeld;
    private bool dragging;
    private float pressX;
    private float previousX;
    private bool wasReady;
    private float readySince;

    private void Awake()
    {
        if (climber == null && !TryGetComponent(out climber))
        {
            Debug.LogError($"{nameof(ClimberInput)} on '{name}' has no {nameof(TowerClimber)} assigned.", this);
            enabled = false;
            return;
        }

#if !ENABLE_INPUT_SYSTEM
        Input.simulateMouseWithTouches = false;
#endif
    }

    private void OnDisable()
    {
        pointerHeld = false;
        dragging = false;
    }

    private void Update()
    {
        bool pressedThisFrame = false;

        if (ReadPointer(out bool down, out bool held, out float x))
        {
            if (down && !IsPointerOverUI())
            {
                pointerHeld = true;
                dragging = false;
                pressX = previousX = x;
                pressedThisFrame = true;
            }
            else if (held && pointerHeld)
            {
                UpdateSwipe(x);
            }
        }

        if (!held)
        {
            pointerHeld = false;
            dragging = false;
        }

        climber.SetHorizontalInput(ReadKeyboardSteering());
        UpdateClimb(pressedThisFrame || ReadKeyboardClimbPressed(), pointerHeld || ReadKeyboardClimbHeld());
    }

    private void UpdateSwipe(float x)
    {
        if (!dragging && Mathf.Abs(x - pressX) >= swipeDeadZonePixels)
        {
            dragging = true;
            previousX = x;
            return;
        }

        if (!dragging)
            return;

        float deltaPixels = x - previousX;
        previousX = x;

        if (Screen.width > 0)
            climber.AddSteeringDegrees(deltaPixels / Screen.width * degreesPerScreenWidth);
    }

    private void UpdateClimb(bool pressedThisFrame, bool holding)
    {
        bool ready = climber.CanStartClimb;
        if (ready && !wasReady)
            readySince = Time.time;
        wasReady = ready;

        if (!ready || !holding)
            return;

        if (pressedThisFrame || Time.time - readySince >= climbRepeatDelay)
            climber.TryJump();
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

#if ENABLE_INPUT_SYSTEM
    private static bool ReadPointer(out bool down, out bool held, out float x)
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && (touchscreen.primaryTouch.press.isPressed || touchscreen.primaryTouch.press.wasReleasedThisFrame))
        {
            var touch = touchscreen.primaryTouch;
            down = touch.press.wasPressedThisFrame;
            held = touch.press.isPressed;
            x = touch.position.ReadValue().x;
            return true;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            down = mouse.leftButton.wasPressedThisFrame;
            held = mouse.leftButton.isPressed;
            x = mouse.position.ReadValue().x;
            return true;
        }

        down = held = false;
        x = 0f;
        return false;
    }

    private static float ReadKeyboardSteering()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return 0f;

        float value = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            value -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            value += 1f;
        return value;
    }

    private static bool ReadKeyboardClimbPressed() => Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

    private static bool ReadKeyboardClimbHeld() => Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
#else
    private static bool ReadPointer(out bool down, out bool held, out float x)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            down = touch.phase == TouchPhase.Began;
            held = touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
            x = touch.position.x;
            return true;
        }

        down = Input.GetMouseButtonDown(0);
        held = Input.GetMouseButton(0);
        x = Input.mousePosition.x;
        return true;
    }

    private static float ReadKeyboardSteering()
    {
        float value = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            value -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            value += 1f;
        return value;
    }

    private static bool ReadKeyboardClimbPressed() => Input.GetKeyDown(KeyCode.Space);

    private static bool ReadKeyboardClimbHeld() => Input.GetKey(KeyCode.Space);
#endif
}
