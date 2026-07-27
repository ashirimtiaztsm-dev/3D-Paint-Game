using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPaintTarget", menuName = "ColorGame/Paint Target Definition")]
public class PaintTargetDefinition : ScriptableObject
{
    [Serializable]
    public sealed class Region
    {
        [SerializeField] private PaintColorDefinition requiredPaint;
        [SerializeField] private Texture2D maskTexture;
        [SerializeField, Range(0f, 1f)] private float maskThreshold = 0.5f;

        public PaintColorDefinition RequiredPaint => requiredPaint;
        public Texture2D MaskTexture => maskTexture;
        public float MaskThreshold => maskThreshold;
    }

    [Header("Presentation")]
    [SerializeField] private string displayName;
    [SerializeField] private Sprite previewSprite;

    [Header("Required Regions")]
    [SerializeField] private List<Region> regions = new List<Region>();

    [Header("Completion")]
    [SerializeField, Range(0.5f, 1f)] private float requiredCompletion = 0.95f;

    public string DisplayName => displayName;
    public Sprite PreviewSprite => previewSprite;
    public IReadOnlyList<Region> Regions => regions;
    public float RequiredCompletion => requiredCompletion;

    private void OnValidate()
    {
        requiredCompletion = Mathf.Clamp(requiredCompletion, 0.5f, 1f);
    }
}
