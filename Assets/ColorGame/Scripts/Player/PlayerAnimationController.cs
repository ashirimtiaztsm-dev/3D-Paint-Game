using UnityEngine;

// Drives the character Animator purely from existing gameplay state (movement + firing) — no
// input reading, no gameplay logic of its own. MoveSpeed comes from PlayerMovementController's
// normalized horizontal speed; IsSpraying mirrors PaintGunFireController's fire session state.
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovementController movementController;
    [SerializeField] private PaintGunFireController fireController;

    [Header("Movement Blending")]
    [SerializeField] private float movementDampTime = 0f;
    [SerializeField, Range(0f, 0.5f)] private float movementThreshold = 0.05f;

    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int IsSprayingHash = Animator.StringToHash("IsSpraying");

    private void Awake()
    {
        if (animator == null)
            Debug.LogWarning($"{nameof(PlayerAnimationController)} on '{name}' has no {nameof(animator)} assigned.", this);

        if (movementController == null)
            Debug.LogWarning($"{nameof(PlayerAnimationController)} on '{name}' has no {nameof(movementController)} assigned.", this);

        if (fireController == null)
            Debug.LogWarning($"{nameof(PlayerAnimationController)} on '{name}' has no {nameof(fireController)} assigned.", this);
    }

    private void OnEnable()
    {
        if (fireController != null)
        {
            fireController.FireStarted += HandleFireStarted;
            fireController.FireStopped += HandleFireStopped;
        }
    }

    private void OnDisable()
    {
        if (fireController != null)
        {
            fireController.FireStarted -= HandleFireStarted;
            fireController.FireStopped -= HandleFireStopped;
        }

        // Never leave the spray pose latched while this component (or the object it lives on) is
        // disabled — matches PaintGunFireController's own OnDisable/StopFiring guarantee.
        if (animator != null)
            animator.SetBool(IsSprayingHash, false);
    }

    private void Update()
    {
        if (animator == null || movementController == null)
            return;

        float rawSpeed = movementController.NormalizedHorizontalSpeed;
        float speed = rawSpeed < movementThreshold ? 0f : rawSpeed;
        animator.SetFloat(MoveSpeedHash, speed, movementDampTime, Time.deltaTime);
    }

    private void HandleFireStarted()
    {
        if (animator != null)
            animator.SetBool(IsSprayingHash, true);
    }

    private void HandleFireStopped()
    {
        if (animator != null)
            animator.SetBool(IsSprayingHash, false);
    }
}
