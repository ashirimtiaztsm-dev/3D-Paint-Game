using System;
using UnityEngine;

// Owns the hold-to-fill transfer loop: watches the selected interaction zone and the contextual
// hold state, and moves paint from the resolved PaintTank into the Player's PaintGunReservoir while
// (and only while) a valid Fill hold is active. No paint-quantity policy lives here — that's inside
// PaintGunReservoir; this script only decides *whether* and *how much* to request each frame.
public class PaintFillController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInteractionDetector detector;
    [SerializeField] private ContextualActionUI actionUI;
    [SerializeField] private PaintGunReservoir reservoir;

    private PaintTank activeTank;
    private bool isFilling;

    public event Action FillStarted;
    public event Action FillStopped;
    public event Action FillCompleted;
    public event Action<PaintTank> ActiveTankChanged;

    private void Awake()
    {
        if (detector == null)
            Debug.LogWarning($"{nameof(PaintFillController)} on '{name}' has no {nameof(detector)} assigned.", this);

        if (actionUI == null)
            Debug.LogWarning($"{nameof(PaintFillController)} on '{name}' has no {nameof(actionUI)} assigned.", this);

        if (reservoir == null)
            Debug.LogWarning($"{nameof(PaintFillController)} on '{name}' has no {nameof(reservoir)} assigned.", this);
    }

    private void OnEnable()
    {
        if (detector != null)
        {
            detector.SelectedZoneChanged += HandleSelectedZoneChanged;
            ResolveActiveTank(detector.CurrentZone);
        }
    }

    private void OnDisable()
    {
        if (detector != null)
            detector.SelectedZoneChanged -= HandleSelectedZoneChanged;

        StopFilling();
        SetActiveTank(null);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            StopFilling();
    }

    private void Update()
    {
        if (activeTank == null || reservoir == null)
            return;

        UpdateButtonInteractable();

        bool wantsToFill = actionUI != null
            && actionUI.CurrentActionType == InteractionActionType.Fill
            && actionUI.IsCurrentActionHeld;

        bool canAcceptMore = CanAcceptMore(activeTank.PaintDefinition);

        if (!wantsToFill || !activeTank.HasPaintAvailable || !canAcceptMore)
        {
            StopFilling();
            return;
        }

        if (!isFilling)
        {
            isFilling = true;
            FillStarted?.Invoke();
        }

        TransferPaint();
    }

    private void HandleSelectedZoneChanged(PlayerInteractionZone zone)
    {
        ResolveActiveTank(zone);
    }

    private void ResolveActiveTank(PlayerInteractionZone zone)
    {
        if (zone == null || zone.ActionType != InteractionActionType.Fill)
        {
            SetActiveTank(null);
            return;
        }

        // Resolved once per zone-selection change and cached — never searched again per frame.
        PaintTank tank = zone.GetComponentInParent<PaintTank>();
        SetActiveTank(tank);
    }

    private void SetActiveTank(PaintTank tank)
    {
        if (activeTank == tank)
            return;

        StopFilling();
        activeTank = tank;
        ActiveTankChanged?.Invoke(activeTank);
    }

    private void StopFilling()
    {
        if (!isFilling)
            return;

        isFilling = false;
        FillStopped?.Invoke();
    }

    private void TransferPaint()
    {
        PaintColorDefinition tankColor = activeTank.PaintDefinition;
        if (tankColor == null)
            return;

        bool isColorChange = reservoir.CurrentPaint != null && reservoir.CurrentPaint != tankColor;

        // A colour-change fill clears the reservoir as part of the transfer, so the space available
        // for THIS request is the full capacity, not whatever room the old colour currently leaves.
        float availableSpace = isColorChange ? reservoir.MaximumCapacity : (reservoir.MaximumCapacity - reservoir.CurrentAmount);
        if (availableSpace <= 0f)
            return;

        float requested = Mathf.Min(activeTank.TransferRate * Time.deltaTime, availableSpace);
        if (requested <= 0f)
            return;

        // The tank must provide the paint before it is added — never take more from a finite tank
        // than the reservoir can actually accept.
        float taken = activeTank.TakePaint(requested);
        if (taken <= 0f)
        {
            StopFilling();
            return;
        }

        // A colour-change transfer can never already be "full" going in, since it starts from zero.
        bool wasFullBefore = reservoir.IsFull && !isColorChange;

        reservoir.AddPaint(tankColor, taken);

        if (!wasFullBefore && reservoir.IsFull)
            FillCompleted?.Invoke();
    }

    private void UpdateButtonInteractable()
    {
        if (actionUI == null || actionUI.CurrentActionType != InteractionActionType.Fill)
            return;

        bool canAccept = CanAcceptMore(activeTank.PaintDefinition);
        actionUI.ActiveButton?.SetInteractable(canAccept);
    }

    private bool CanAcceptMore(PaintColorDefinition tankColor)
    {
        if (reservoir.CurrentPaint == null || reservoir.CurrentPaint != tankColor)
            return true; // empty, or a different colour that would replace it — always has "room"

        return !reservoir.IsFull;
    }
}
