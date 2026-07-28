using System;
using System.Collections.Generic;
using UnityEngine;

// Designer-authored data only — never stores runtime progress. PaintCoverageTracker owns all
// runtime region/progress state; this asset only describes what a level requires.
[CreateAssetMenu(fileName = "NewPaintTarget", menuName = "ColorGame/Paint Target Definition")]
public class PaintTargetDefinition : ScriptableObject
{
    [Serializable]
    public sealed class Region
    {
        [SerializeField] private string displayName;
        [SerializeField] private PaintColorDefinition requiredPaint;
        [SerializeField] private Texture2D maskTexture;
        [SerializeField, Range(0f, 1f)] private float maskThreshold = 0.5f;
        [SerializeField, Range(0.5f, 1f)] private float requiredCompletion = 0.95f;
        [SerializeField] private Sprite icon;
        [SerializeField] private bool useUIColorOverride;
        [SerializeField] private Color uiColorOverride = Color.white;

        public string DisplayName => displayName;
        public PaintColorDefinition RequiredPaint => requiredPaint;
        public Texture2D MaskTexture => maskTexture;
        public float MaskThreshold => maskThreshold;
        public float RequiredCompletion => requiredCompletion;
        public Sprite Icon => icon;

        public Color EffectiveUIColor => useUIColorOverride
            ? uiColorOverride
            : (requiredPaint != null ? requiredPaint.DisplayColor : Color.white);

        // A region is only usable if it has real paint identity, a mask to evaluate, and a sane
        // completion threshold — anything else must be skipped rather than silently mis-scored.
        public bool IsValidConfiguration =>
            requiredPaint != null
            && requiredPaint.ColorId != PaintColorId.None
            && maskTexture != null
            && requiredCompletion > 0f
            && requiredCompletion <= 1f;
    }

    [Header("Identity")]
    [SerializeField] private string targetId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite previewSprite;

    [Header("Required Regions")]
    [SerializeField] private List<Region> regions = new List<Region>();

    public string TargetId => targetId;
    public string DisplayName => displayName;
    public Sprite PreviewSprite => previewSprite;
    public IReadOnlyList<Region> Regions => regions;
}
