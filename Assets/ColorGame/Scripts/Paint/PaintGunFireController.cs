using System;
using UnityEngine;

// Owns the hold-to-fire loop: decides whether firing should be happening this frame and, if so,
// consumes paint and forwards the resulting hit to PaintSprayer. No raycasting or particle logic
// lives here — that's PaintSprayer's job; this script only decides *whether* and *how much*.
public class PaintGunFireController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInteractionDetector detector;
    [SerializeField] private ContextualActionUI actionUI;
    [SerializeField] private PaintGunReservoir reservoir;
    [SerializeField] private PaintSprayer sprayer;

    [Header("Firing")]
    [SerializeField] private float consumptionRate = 15f;

    private bool isFiring;
    private PaintSurfaceMarker lastHitSurface;

    public bool IsFiring => isFiring;

    public event Action FireStarted;
    public event Action FireStopped;
    public event Action PaintDepleted;
    public event Action<PaintSprayHit> SprayHit;

    private void Awake()
    {
        if (detector == null)
            Debug.LogWarning($"{nameof(PaintGunFireController)} on '{name}' has no {nameof(detector)} assigned.", this);

        if (actionUI == null)
            Debug.LogWarning($"{nameof(PaintGunFireController)} on '{name}' has no {nameof(actionUI)} assigned.", this);

        if (reservoir == null)
            Debug.LogWarning($"{nameof(PaintGunFireController)} on '{name}' has no {nameof(reservoir)} assigned.", this);

        if (sprayer == null)
            Debug.LogWarning($"{nameof(PaintGunFireController)} on '{name}' has no {nameof(sprayer)} assigned.", this);
    }

    private void OnEnable()
    {
        if (detector != null)
            detector.SelectedZoneChanged += HandleSelectedZoneChanged;

        if (reservoir != null)
            reservoir.AmountChanged += HandleReservoirAmountChanged;

        UpdateButtonInteractable();
    }

    private void OnDisable()
    {
        if (detector != null)
            detector.SelectedZoneChanged -= HandleSelectedZoneChanged;

        if (reservoir != null)
            reservoir.AmountChanged -= HandleReservoirAmountChanged;

        StopFiring();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            StopFiring();
    }

    private void Update()
    {
        bool isFireZoneSelected = actionUI != null && actionUI.CurrentActionType == InteractionActionType.Fire;

        if (!isFireZoneSelected)
        {
            StopFiring();
            return;
        }

        // Deferred to Update() rather than the zone-changed event itself: PlayerInteractionDetector's
        // SelectedZoneChanged has multiple subscribers (this, ContextualActionUI, PaintFillController),
        // and subscriber order is not guaranteed — reading actionUI.CurrentActionType synchronously
        // inside that same event could see a stale value if ContextualActionUI hasn't run yet. By the
        // time Update() ticks, all same-frame event dispatches have already settled.
        UpdateButtonInteractable();

        bool wantsToFire = actionUI.IsCurrentActionHeld;
        bool hasPaint = reservoir != null && !reservoir.IsEmpty;

        if (!wantsToFire || !hasPaint || sprayer == null || reservoir == null)
        {
            StopFiring();
            return;
        }

        bool hasValidHit = sprayer.TryGetValidHit(out RaycastHit hit, out PaintSurfaceMarker surface);

        if (!isFiring)
        {
            isFiring = true;
            sprayer.BeginSprayVisual(reservoir.CurrentPaint);
            FireStarted?.Invoke();
        }

        if (!hasValidHit)
        {
            // Session stays active (still "firing") but nothing is landing on a surface — hide the
            // impact and consume no paint, per "do not waste paint when the ray misses".
            sprayer.HideImpactVisual();
            InterruptStroke();
            return;
        }

        if (surface != lastHitSurface)
            InterruptStroke();

        float amountBeforeConsumption = reservoir.CurrentAmount;
        float requestedConsumption = consumptionRate * Time.deltaTime;
        float actualConsumption = reservoir.ConsumePaint(requestedConsumption);

        if (actualConsumption <= 0f)
        {
            sprayer.HideImpactVisual();
            InterruptStroke();
            return;
        }

        PaintColorDefinition paint = reservoir.CurrentPaint;
        sprayer.EmitSprayHit(paint, actualConsumption, hit, surface);
        lastHitSurface = surface;

        SprayHit?.Invoke(new PaintSprayHit(paint, actualConsumption, hit.point, hit.normal, hit.textureCoord, hit.collider));

        // Edge-triggered: fires exactly once, only when THIS controller's own consumption during
        // active firing is what drained the last bit of paint. Never triggered by Clear(), level
        // resets, or colour-replacement mutating the reservoir from outside this loop.
        bool depletedByThisConsumption = amountBeforeConsumption > 0f && actualConsumption > 0f && reservoir.CurrentAmount <= 0f;

        if (depletedByThisConsumption)
            PaintDepleted?.Invoke();
    }

    private void HandleSelectedZoneChanged(PlayerInteractionZone zone)
    {
        StopFiring();
    }

    private void HandleReservoirAmountChanged(float currentAmount)
    {
        UpdateButtonInteractable();
    }

    private void InterruptStroke()
    {
        if (lastHitSurface != null)
        {
            lastHitSurface.NotifyStrokeInterrupted();
            lastHitSurface = null;
        }
    }

    private void UpdateButtonInteractable()
    {
        if (actionUI == null || actionUI.CurrentActionType != InteractionActionType.Fire)
            return;

        bool interactable = reservoir != null && !reservoir.IsEmpty;
        actionUI.ActiveButton?.SetInteractable(interactable);
    }

    private void StopFiring()
    {
        sprayer?.StopSprayVisual();
        InterruptStroke();

        if (!isFiring)
            return;

        isFiring = false;
        FireStopped?.Invoke();
    }
}
