using UnityEngine;

// Drives the character Animator from existing gameplay state.
// Movement uses a simple Idle/Move bool. Any meaningful world movement plays
// the complete in-place movement clip at its normal speed.
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovementController movementController;
    [SerializeField] private PaintGunFireController fireController;

    [Header("Movement Detection")]
    [SerializeField] private float startMovingSpeed = 0.05f;
    [SerializeField] private float stopMovingSpeed = 0.02f;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsSprayingHash = Animator.StringToHash("IsSpraying");

    private bool isMoving;

    private void Awake()
    {
        if (animator == null)
            Debug.LogWarning($"{nameof(PlayerAnimationController)} on '{name}' has no {nameof(animator)} assigned.", this);

        if (movementController == null)
            Debug.LogWarning($"{nameof(PlayerAnimationController)} on '{name}' has no {nameof(movementController)} assigned.", this);

        if (fireController == null)
            Debug.LogWarning($"{nameof(PlayerAnimationController)} on '{name}' has no {nameof(fireController)} assigned.", this);

        startMovingSpeed = Mathf.Max(0f, startMovingSpeed);
        stopMovingSpeed = Mathf.Clamp(stopMovingSpeed, 0f, startMovingSpeed);
    }

    private void OnEnable()
    {
        if (fireController != null)
        {
            fireController.FireStarted += HandleFireStarted;
            fireController.FireStopped += HandleFireStopped;
        }

        isMoving = false;

        if (animator != null)
        {
            animator.SetBool(IsMovingHash, false);
            animator.SetBool(IsSprayingHash, fireController != null && fireController.IsFiring);
        }
    }

    private void OnDisable()
    {
        if (fireController != null)
        {
            fireController.FireStarted -= HandleFireStarted;
            fireController.FireStopped -= HandleFireStopped;
        }

        isMoving = false;

        if (animator != null)
        {
            animator.SetBool(IsMovingHash, false);
            animator.SetBool(IsSprayingHash, false);
        }
    }

    private void Update()
    {
        if (animator == null || movementController == null)
            return;

        float horizontalSpeed = movementController.HorizontalSpeed;

        bool shouldMove = isMoving
            ? horizontalSpeed > stopMovingSpeed
            : horizontalSpeed > startMovingSpeed;

        if (shouldMove == isMoving)
            return;

        isMoving = shouldMove;
        animator.SetBool(IsMovingHash, isMoving);
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

    private void OnValidate()
    {
        startMovingSpeed = Mathf.Max(0f, startMovingSpeed);
        stopMovingSpeed = Mathf.Clamp(stopMovingSpeed, 0f, startMovingSpeed);
    }
}
