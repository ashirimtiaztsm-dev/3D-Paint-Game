using UnityEngine;

// Immutable data describing one successful spray sample. Carries the UV coordinate of the hit so a
// later stage can stamp it into a RenderTexture — this struct itself never touches any texture.
public readonly struct PaintSprayHit
{
    public PaintSprayHit(
        PaintColorDefinition paint,
        float paintAmount,
        Vector3 point,
        Vector3 normal,
        Vector2 textureCoordinate,
        Collider collider)
    {
        Paint = paint;
        PaintAmount = paintAmount;
        Point = point;
        Normal = normal;
        TextureCoordinate = textureCoordinate;
        Collider = collider;
    }

    public PaintColorDefinition Paint { get; }
    public float PaintAmount { get; }
    public Vector3 Point { get; }
    public Vector3 Normal { get; }
    public Vector2 TextureCoordinate { get; }
    public Collider Collider { get; }
}
