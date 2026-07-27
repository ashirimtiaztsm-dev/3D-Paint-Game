using UnityEngine;

// Describes one actual brush stamp applied by PaintableSurface, including interpolated stamps.
// PaintCoverageTracker consumes this data to maintain a low-resolution CPU evaluation grid.
public readonly struct PaintStampData
{
    public PaintStampData(
        PaintColorDefinition paint,
        Vector2 textureCoordinate,
        float radiusUV,
        float hardness,
        float opacity)
    {
        Paint = paint;
        TextureCoordinate = textureCoordinate;
        RadiusUV = radiusUV;
        Hardness = hardness;
        Opacity = opacity;
    }

    public PaintColorDefinition Paint { get; }
    public Vector2 TextureCoordinate { get; }
    public float RadiusUV { get; }
    public float Hardness { get; }
    public float Opacity { get; }
}
