using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MobileInputReader inputReader;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform characterVisual;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 25f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField, Range(0f, 0.9f)] private float inputDeadZone = 0.1f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedForce = -2f;

    private CharacterController characterController;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private bool movementEnabled = true;

    // Read-only animation-facing state, computed from the actual horizontalVelocity (never from
    // raw joystick input) — reflects real acceleration/deceleration, not just intent.
    public float HorizontalSpeed => horizontalVelocity.magnitude;

    public float NormalizedHorizontalSpeed => moveSpeed > 0f
        ? Mathf.Clamp01(horizontalVelocity.magnitude / moveSpeed)
        : 0f;

    public bool IsMoving => horizontalVelocity.sqrMagnitude > 0.0025f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (inputReader == null)
            Debug.LogWarning($"{nameof(PlayerMovementController)} on '{name}' has no {nameof(MobileInputReader)} assigned.", this);

        if (cameraTransform == null)
            Debug.LogWarning($"{nameof(PlayerMovementController)} on '{name}' has no camera Transform assigned.", this);

        if (characterVisual == null)
            Debug.LogWarning($"{nameof(PlayerMovementController)} on '{name}' has no character visual assigned.", this);
    }

    private void Update()
    {
        Vector3 desiredDirection = GetCameraRelativeDirection();

        UpdateHorizontalVelocity(desiredDirection);
        RotateVisual(desiredDirection);
        ApplyGravity();
        Move();
    }

    // Exposed for cutscene / level-completion stages to freeze movement. Zeroes horizontal velocity
    // immediately on disable so movement stops on the same frame instead of coasting through the
    // usual deceleration ramp; vertical velocity (gravity) is left untouched.
    public void SetMovementEnabled(bool isMovementEnabled)
    {
        movementEnabled = isMovementEnabled;

        if (!movementEnabled)
            horizontalVelocity = Vector3.zero;
    }

    private Vector3 GetCameraRelativeDirection()
    {
        if (!movementEnabled || inputReader == null)
            return Vector3.zero;

        Vector2 input = inputReader.MovementInput;
        if (input.magnitude < inputDeadZone)
            return Vector3.zero;

        Vector3 forward = Flatten(cameraTransform != null ? cameraTransform.forward : Vector3.forward, Vector3.forward);
        Vector3 right = Flatten(cameraTransform != null ? cameraTransform.right : Vector3.right, Vector3.right);

        Vector3 direction = forward * input.y + right * input.x;
        return Vector3.ClampMagnitude(direction, 1f);
    }

    private static Vector3 Flatten(Vector3 direction, Vector3 fallback)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : fallback;
    }

    private void UpdateHorizontalVelocity(Vector3 desiredDirection)
    {
        Vector3 targetVelocity = desiredDirection * moveSpeed;
        float rate = desiredDirection.sqrMagnitude > 0.0001f ? acceleration : deceleration;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, rate * Time.deltaTime);
    }

    private void RotateVisual(Vector3 desiredDirection)
    {
        if (characterVisual == null || desiredDirection.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(desiredDirection, Vector3.up);
        characterVisual.rotation = Quaternion.Slerp(characterVisual.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = groundedForce;

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void Move()
    {
        Vector3 motion = horizontalVelocity;
        motion.y = verticalVelocity;
        characterController.Move(motion * Time.deltaTime);
    }
}
