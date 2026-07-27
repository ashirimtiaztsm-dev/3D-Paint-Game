using UnityEngine;
using UnityEngine.InputSystem;

// Shows the correct contextual action button for whatever PlayerInteractionDetector currently has
// selected. Subscribes to the detector's event rather than polling scene objects. Contains no
// Fill/Fire gameplay logic — only visibility and combined pointer/keyboard hold-state reporting.
public class ContextualActionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInteractionDetector detector;

    [Header("Fill Button")]
    [SerializeField] private GameObject fillButtonRoot;
    [SerializeField] private HoldActionButton fillButton;

    [Header("Fire Button")]
    [SerializeField] private GameObject fireButtonRoot;
    [SerializeField] private HoldActionButton fireButton;

    private InteractionActionType currentActionType = InteractionActionType.None;
    private bool spaceHeld;

    public HoldActionButton ActiveButton => currentActionType switch
    {
        InteractionActionType.Fill => fillButton,
        InteractionActionType.Fire => fireButton,
        _ => null
    };

    public InteractionActionType CurrentActionType => currentActionType;

    public bool IsCurrentActionHeld
    {
        get
        {
            // Guard against None explicitly rather than relying solely on Update() keeping spaceHeld
            // in sync — Space must never read as "held" when no contextual action is available.
            if (currentActionType == InteractionActionType.None)
                return false;

            HoldActionButton active = ActiveButton;
            return (active != null && active.IsHeld) || spaceHeld;
        }
    }

    private void Awake()
    {
        if (detector == null)
            Debug.LogWarning($"{nameof(ContextualActionUI)} on '{name}' has no {nameof(detector)} assigned.", this);

        SetActive(fillButtonRoot, false);
        SetActive(fireButtonRoot, false);
    }

    private void OnEnable()
    {
        if (detector != null)
        {
            detector.SelectedZoneChanged += HandleSelectedZoneChanged;
            ApplyActionType(ResolveActionType(detector.CurrentZone));
        }
    }

    private void OnDisable()
    {
        if (detector != null)
            detector.SelectedZoneChanged -= HandleSelectedZoneChanged;

        // Force-release both buttons and clear the keyboard fallback so IsCurrentActionHeld cannot
        // read as true while this component is disabled.
        fillButton?.ForceRelease();
        fireButton?.ForceRelease();
        spaceHeld = false;
    }

    private void Update()
    {
        // Editor/desktop fallback: Space counts as holding whichever action is currently visible,
        // and does nothing when no contextual action is available. Kept entirely separate from
        // HoldActionButton's own pointer-based state machine; combined only through the property above.
        Keyboard keyboard = Keyboard.current;
        bool spacePressed = keyboard != null && keyboard.spaceKey.isPressed;
        spaceHeld = currentActionType != InteractionActionType.None && spacePressed;
    }

    private void HandleSelectedZoneChanged(PlayerInteractionZone zone)
    {
        ApplyActionType(ResolveActionType(zone));
    }

    private static InteractionActionType ResolveActionType(PlayerInteractionZone zone)
    {
        return zone != null ? zone.ActionType : InteractionActionType.None;
    }

    private void ApplyActionType(InteractionActionType newType)
    {
        if (newType == currentActionType)
            return;

        // Release and hide whatever action was showing before so no stale held state survives the swap.
        if (currentActionType == InteractionActionType.Fill)
        {
            fillButton?.ForceRelease();
            SetActive(fillButtonRoot, false);
        }
        else if (currentActionType == InteractionActionType.Fire)
        {
            fireButton?.ForceRelease();
            SetActive(fireButtonRoot, false);
        }

        currentActionType = newType;
        spaceHeld = false;

        if (currentActionType == InteractionActionType.Fill)
            SetActive(fillButtonRoot, true);
        else if (currentActionType == InteractionActionType.Fire)
            SetActive(fireButtonRoot, true);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
