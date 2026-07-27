using UnityEngine;

// Describes one fillable paint source. Never touches the Player's reservoir directly —
// PaintFillController is the only thing that reads from a tank and writes into the reservoir.
public class PaintTank : MonoBehaviour
{
    [Header("Paint")]
    [SerializeField] private PaintColorDefinition paintDefinition;
    [SerializeField] private float transferRate = 40f;
    [SerializeField] private bool infiniteSupply = true;
    [SerializeField] private float availableQuantity = 500f;

    [Header("References")]
    [SerializeField] private PlayerInteractionZone interactionZone;
    [SerializeField] private Renderer visualRenderer;

    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;

    public PaintColorDefinition PaintDefinition => paintDefinition;
    public float TransferRate => transferRate;
    public float AvailableQuantity => availableQuantity;
    public bool IsInfiniteSupply => infiniteSupply;
    public bool HasPaintAvailable => infiniteSupply || availableQuantity > 0f;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();

        if (paintDefinition == null)
            Debug.LogWarning($"{nameof(PaintTank)} on '{name}' has no {nameof(paintDefinition)} assigned.", this);

        if (interactionZone == null)
            Debug.LogWarning($"{nameof(PaintTank)} on '{name}' has no {nameof(interactionZone)} assigned.", this);

        // A finite tank that starts already empty must never be selectable by the detector.
        // The visual stays as-is; only the interaction zone's availability is synced.
        SyncInteractionAvailability();
        ApplyVisualColor();
    }

    private void SyncInteractionAvailability()
    {
        interactionZone?.SetAvailable(HasPaintAvailable);
    }

    // Returns the amount actually provided (0 if unavailable). Infinite tanks never reduce supply;
    // finite tanks reduce it safely and mark their zone unavailable once depleted.
    public float TakePaint(float requestedAmount)
    {
        if (requestedAmount <= 0f || paintDefinition == null)
            return 0f;

        if (infiniteSupply)
            return requestedAmount;

        if (availableQuantity <= 0f)
            return 0f;

        float amountTaken = Mathf.Min(requestedAmount, availableQuantity);
        availableQuantity = Mathf.Max(0f, availableQuantity - amountTaken);

        if (availableQuantity <= 0f)
            interactionZone?.SetAvailable(false);

        return amountTaken;
    }

    private void ApplyVisualColor()
    {
        if (visualRenderer == null || paintDefinition == null)
            return;

        visualRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorPropertyId, paintDefinition.DisplayColor);
        propertyBlock.SetColor(ColorPropertyId, paintDefinition.DisplayColor);
        visualRenderer.SetPropertyBlock(propertyBlock);
    }
}
