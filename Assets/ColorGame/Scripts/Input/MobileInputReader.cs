using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MobileInputReader : MonoBehaviour
{
    [Header("Joystick Reference")]
    [SerializeField] private Joystick moveJoystick;

    [Header("Settings")]
    [SerializeField, Range(0f, 0.9f)] private float inputDeadZone = 0.1f;

    private bool movementInputEnabled = true;
    private bool waitingForInputRelease;

    public Vector2 MovementInput { get; private set; }

    private void Awake()
    {
        if (moveJoystick == null)
            Debug.LogWarning($"{nameof(MobileInputReader)} on '{name}' has no joystick assigned. Falling back to keyboard input only.", this);
    }

    private void OnDisable()
    {
        ForceReleaseAllInput();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            ForceReleaseAllInput();
    }

    private void Update()
    {
        if (!movementInputEnabled)
        {
            MovementInput = Vector2.zero;
            return;
        }

        Vector2 rawInput = ReadMovementInput();

        if (waitingForInputRelease)
        {
            if (rawInput.sqrMagnitude > 0f)
            {
                // A finger (or key) held through the disable is still down — ignore it until it
                // genuinely reports zero at least once. Only then does a touch/key count as "fresh".
                MovementInput = Vector2.zero;
                return;
            }

            waitingForInputRelease = false;
        }

        MovementInput = rawInput;
    }

    // Renamed parameter to avoid shadowing Component.enabled. Fully releases the joystick (handle
    // snaps back to centre) via ForceReleaseAllInput, and arms waitingForInputRelease so a finger
    // that was already held before disabling cannot resume movement the instant it's re-enabled —
    // the player must lift and touch (or release and re-press) again.
    public void SetMovementInputEnabled(bool isMovementInputEnabled)
    {
        movementInputEnabled = isMovementInputEnabled;

        if (!movementInputEnabled)
            ForceReleaseAllInput();
    }

    // Stronger reset used for focus-loss, component-disable, and SetMovementInputEnabled(false):
    // force-releases the vendor joystick (via its public OnPointerUp) so no stale touch/handle
    // position survives, and arms waitingForInputRelease so a still-held finger can't resume movement
    // as soon as input is enabled again. Allocating a PointerEventData here is acceptable — these are
    // rare events, not per-frame operations.
    private void ForceReleaseAllInput()
    {
        MovementInput = Vector2.zero;
        waitingForInputRelease = true;

        if (moveJoystick == null)
            return;

        EventSystem currentEventSystem = EventSystem.current;
        if (currentEventSystem == null)
            return;

        moveJoystick.OnPointerUp(new PointerEventData(currentEventSystem));
    }

    private Vector2 ReadMovementInput()
    {
        // Keyboard takes priority so Editor testing works even while a joystick is present in the scene.
        Vector2 keyboardInput = ReadKeyboardInput();
        if (keyboardInput.sqrMagnitude > 0f)
            return ApplyDeadZoneAndClamp(keyboardInput);

        if (moveJoystick != null)
            return ApplyDeadZoneAndClamp(moveJoystick.Direction);

        return Vector2.zero;
    }

    private static Vector2 ReadKeyboardInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return Vector2.zero;

        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            horizontal -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            horizontal += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            vertical -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            vertical += 1f;

        return new Vector2(horizontal, vertical);
    }

    private Vector2 ApplyDeadZoneAndClamp(Vector2 rawInput)
    {
        if (rawInput.magnitude < inputDeadZone)
            return Vector2.zero;

        return Vector2.ClampMagnitude(rawInput, 1f);
    }
}
