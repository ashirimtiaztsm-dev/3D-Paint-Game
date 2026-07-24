using UnityEngine;

public class ThirdPersonOrbitCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private CameraLookInput lookInput;

    [Header("Orbit")]
    [SerializeField] private float distance = 4.5f;
    [SerializeField] private float minimumDistance = 1f;
    [SerializeField] private float yawSensitivity = 0.2f;
    [SerializeField] private float pitchSensitivity = 0.2f;
    [SerializeField] private float minPitch = 10f;
    [SerializeField] private float maxPitch = 75f;

    [Header("Starting Angles")]
    [SerializeField] private float startYaw = 0f;
    [SerializeField] private float startPitch = 18f;

    [Header("Smoothing")]
    [SerializeField] private float followSmoothTime = 0.12f;
    [SerializeField] private float rotationSmoothSpeed = 18f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float collisionRadius = 0.25f;
    [SerializeField] private float wallPadding = 0.15f;
    [SerializeField] private float distanceRestoreSpeed = 8f;

    private float targetYaw;
    private float targetPitch;
    private float smoothedYaw;
    private float smoothedPitch;
    private float currentDistance;
    private Vector3 followVelocity;

    private bool cameraInputEnabled = true;
    private bool externalControlActive;
    private bool cameraInputEnabledBeforeExternalControl;

    private void Awake()
    {
        targetYaw = startYaw;
        targetPitch = startPitch;
        smoothedYaw = startYaw;
        smoothedPitch = startPitch;
        currentDistance = distance;

        if (target == null)
            Debug.LogWarning($"{nameof(ThirdPersonOrbitCamera)} on '{name}' has no target assigned.", this);

        if (lookInput == null)
            Debug.LogWarning($"{nameof(ThirdPersonOrbitCamera)} on '{name}' has no {nameof(CameraLookInput)} assigned.", this);
    }

    private void LateUpdate()
    {
        if (externalControlActive || target == null)
            return;

        ApplyLookInput();

        smoothedYaw = Mathf.LerpAngle(smoothedYaw, targetYaw, rotationSmoothSpeed * Time.deltaTime);
        smoothedPitch = Mathf.LerpAngle(smoothedPitch, targetPitch, rotationSmoothSpeed * Time.deltaTime);

        // Euler z is hard-coded to 0 here and everywhere below, so the camera can never roll.
        Quaternion orientation = Quaternion.Euler(smoothedPitch, smoothedYaw, 0f);
        Vector3 pivot = target.position;
        Vector3 backDirection = orientation * Vector3.back;

        float resolvedDistance = ResolveTargetDistance(pivot, backDirection);
        bool isNewObstruction = resolvedDistance < currentDistance;

        if (isNewObstruction)
        {
            // A wall just got closer (or appeared). SmoothDamp-ing the position here would let the
            // camera lag behind for a few frames and clip through the new geometry, so snap the
            // position immediately and drop any residual follow velocity to avoid an overshoot
            // once normal smoothing resumes.
            currentDistance = resolvedDistance;
            transform.position = pivot + backDirection * currentDistance;
            followVelocity = Vector3.zero;
        }
        else
        {
            // Unobstructed following, or an obstruction clearing — both stay smooth.
            currentDistance = Mathf.Lerp(currentDistance, resolvedDistance, distanceRestoreSpeed * Time.deltaTime);
            Vector3 desiredPosition = pivot + backDirection * currentDistance;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, followSmoothTime);
        }

        transform.rotation = orientation;
    }

    // Renamed parameter to avoid shadowing Component.enabled.
    // Also disables/resets CameraLookInput itself (not just this component's own flag), so disabling
    // clears any accumulated drag delta, releases the active pointer, and blocks right-mouse input too.
    public void SetCameraInputEnabled(bool isCameraInputEnabled)
    {
        cameraInputEnabled = isCameraInputEnabled;

        if (lookInput != null)
            lookInput.SetLookEnabled(isCameraInputEnabled);
    }

    // Hands the transform fully to an external controller (e.g. a future level-complete cutscene camera);
    // LateUpdate stops touching position/rotation entirely so nothing fights over the transform.
    public void BeginExternalControl()
    {
        // Guard against a second Begin call overwriting the saved pre-cutscene state.
        if (!externalControlActive)
            cameraInputEnabledBeforeExternalControl = cameraInputEnabled;

        externalControlActive = true;
        lookInput?.SetLookEnabled(false);
    }

    public void EndExternalControl()
    {
        if (!externalControlActive)
            return;

        externalControlActive = false;

        // Restore whatever camera-input state was in effect before the cutscene began, rather than
        // unconditionally re-enabling — if input was already disabled going in, it stays disabled.
        SetCameraInputEnabled(cameraInputEnabledBeforeExternalControl);
    }

    private void ApplyLookInput()
    {
        Vector2 delta = lookInput != null ? lookInput.ConsumeLookDelta() : Vector2.zero;

        if (!cameraInputEnabled)
            return;

        targetYaw += delta.x * yawSensitivity;
        targetPitch -= delta.y * pitchSensitivity;
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
    }

    private float ResolveTargetDistance(Vector3 pivot, Vector3 backDirection)
    {
        if (Physics.SphereCast(pivot, collisionRadius, backDirection, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
            return Mathf.Max(minimumDistance, hit.distance - wallPadding);

        return distance;
    }
}
