using UnityEngine;

// Temporary visualization of the gun's current paint: tints a placeholder container Renderer and
// optionally scales a fill mesh vertically by NormalizedAmount. No spray effects — that's a later
// stage. Uses a cached MaterialPropertyBlock so it never allocates a material instance.
public class PaintGunVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PaintGunReservoir reservoir;
    [SerializeField] private Renderer containerRenderer;
    [SerializeField] private Transform fillMesh;

    [Header("Empty State")]
    [SerializeField] private Color emptyColor = new Color(0.6f, 0.6f, 0.6f, 0.35f);

    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock propertyBlock;
    private Vector3 fillMeshBaseScale = Vector3.one;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();

        if (fillMesh != null)
            fillMeshBaseScale = fillMesh.localScale;

        if (reservoir == null)
            Debug.LogWarning($"{nameof(PaintGunVisual)} on '{name}' has no {nameof(reservoir)} assigned.", this);

        if (containerRenderer == null)
            Debug.LogWarning($"{nameof(PaintGunVisual)} on '{name}' has no {nameof(containerRenderer)} assigned.", this);

        ApplyColor(reservoir != null ? reservoir.CurrentPaint : null);
        ApplyFillScale(reservoir != null ? reservoir.NormalizedAmount : 0f);
    }

    private void OnEnable()
    {
        if (reservoir == null)
            return;

        reservoir.PaintColorChanged += HandlePaintColorChanged;
        reservoir.AmountChanged += HandleAmountChanged;
    }

    private void OnDisable()
    {
        if (reservoir == null)
            return;

        reservoir.PaintColorChanged -= HandlePaintColorChanged;
        reservoir.AmountChanged -= HandleAmountChanged;
    }

    private void HandlePaintColorChanged(PaintColorDefinition paint)
    {
        ApplyColor(paint);
    }

    private void HandleAmountChanged(float currentAmount)
    {
        if (reservoir == null)
            return;

        ApplyFillScale(reservoir.NormalizedAmount);

        // Reapply the current colour when refilling from zero with the same paint.
        ApplyColor(reservoir.IsEmpty ? null : reservoir.CurrentPaint);
    }

    private void ApplyColor(PaintColorDefinition paint)
    {
        if (containerRenderer == null)
            return;

        Color color = paint != null ? paint.DisplayColor : emptyColor;

        containerRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorPropertyId, color);
        propertyBlock.SetColor(ColorPropertyId, color);
        containerRenderer.SetPropertyBlock(propertyBlock);
    }

    private void ApplyFillScale(float normalizedAmount)
    {
        if (fillMesh == null)
            return;

        float clamped = Mathf.Clamp01(normalizedAmount);
        fillMesh.localScale = new Vector3(fillMeshBaseScale.x, fillMeshBaseScale.y * clamped, fillMeshBaseScale.z);
    }
}
