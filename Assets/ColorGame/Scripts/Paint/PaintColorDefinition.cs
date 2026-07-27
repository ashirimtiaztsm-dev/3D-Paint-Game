using UnityEngine;

// Static, designer-authored description of one paint colour. Never holds runtime quantity —
// PaintGunReservoir owns that. Gameplay code must compare colours via ColorId or by reference to
// the same definition asset, never by name/tag/hex string/Color equality.
[CreateAssetMenu(fileName = "NewPaintColor", menuName = "ColorGame/Paint Color Definition")]
public class PaintColorDefinition : ScriptableObject
{
    [SerializeField] private PaintColorId colorId = PaintColorId.None;
    [SerializeField] private string displayName;
    [SerializeField] private Color displayColor = Color.white;
    [SerializeField] private Material previewMaterial;
    [SerializeField] private Sprite icon;

    public PaintColorId ColorId => colorId;
    public string DisplayName => displayName;
    public Color DisplayColor => displayColor;
    public Material PreviewMaterial => previewMaterial;
    public Sprite Icon => icon;

    private void OnValidate()
    {
        if (colorId == PaintColorId.None)
            Debug.LogWarning($"{nameof(PaintColorDefinition)} '{name}' has ColorId set to None — assign a real colour.", this);
    }
}
