using System;
using UnityEngine;

// Permanent visual painting for one surface. Every actual GPU brush stamp also raises StampApplied,
// allowing a low-resolution progress tracker to mirror exactly what was stamped without GPU readback.
public class PaintableSurface : MonoBehaviour
{
    private static readonly int PaintTexPropertyId = Shader.PropertyToID("_PaintTex");
    private static readonly int BaseMapPropertyId = Shader.PropertyToID("_BaseMap");

    private static readonly int BrushUVId = Shader.PropertyToID("_BrushUV");
    private static readonly int BrushRadiusId = Shader.PropertyToID("_BrushRadius");
    private static readonly int BrushHardnessId = Shader.PropertyToID("_BrushHardness");
    private static readonly int BrushColorId = Shader.PropertyToID("_BrushColor");
    private static readonly int BrushOpacityId = Shader.PropertyToID("_BrushOpacity");

    [Header("References")]
    [SerializeField] private PaintSurfaceMarker marker;
    [SerializeField] private Renderer surfaceRenderer;
    [SerializeField] private Shader brushShader;
    [SerializeField] private Texture2D baseTexture;

    [Header("Texture")]
    [SerializeField] private int textureResolution = 512;
    [SerializeField] private Color initialClearColor = new Color(0f, 0f, 0f, 0f);

    [Header("Brush")]
    [SerializeField] private float brushRadiusUV = 0.3f;
    [SerializeField, Range(0f, 1f)] private float brushHardness = 0.8f;
    [SerializeField, Range(0f, 1f)] private float brushOpacity = 1f;
    [SerializeField] private float paintAmountMultiplier = 20f;
    [SerializeField, Range(1, 64)] private int maximumInterpolatedStampsPerHit = 24;

    [Header("State")]
    [SerializeField] private bool paintingEnabled = true;

    private RenderTexture currentPaintTexture;
    private RenderTexture workingPaintTexture;
    private Material brushMaterial;
    private MaterialPropertyBlock propertyBlock;

    private bool hasPreviousStrokeUV;
    private Vector2 previousStrokeUV;

    public RenderTexture PaintTexture => currentPaintTexture;
    public bool IsInitialized { get; private set; }

    public event Action<PaintStampData> StampApplied;
    public event Action PaintCleared;

    private void Awake()
    {
        if (marker == null)
            Debug.LogWarning($"{nameof(PaintableSurface)} on '{name}' has no {nameof(marker)} assigned.", this);

        if (surfaceRenderer == null)
            Debug.LogWarning($"{nameof(PaintableSurface)} on '{name}' has no {nameof(surfaceRenderer)} assigned.", this);

        if (brushShader == null)
            Debug.LogWarning($"{nameof(PaintableSurface)} on '{name}' has no {nameof(brushShader)} assigned.", this);

        if (surfaceRenderer == null || brushShader == null)
            return;

        propertyBlock = new MaterialPropertyBlock();
        brushMaterial = new Material(brushShader) { hideFlags = HideFlags.HideAndDontSave };

        currentPaintTexture = CreatePaintRenderTexture();
        workingPaintTexture = CreatePaintRenderTexture();

        ClearRenderTexture(currentPaintTexture, initialClearColor);
        ClearRenderTexture(workingPaintTexture, initialClearColor);

        if (baseTexture != null)
        {
            surfaceRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetTexture(BaseMapPropertyId, baseTexture);
            surfaceRenderer.SetPropertyBlock(propertyBlock);
        }

        IsInitialized = true;
        ApplyPaintTextureToRenderer();
    }

    private void OnEnable()
    {
        if (marker == null)
            return;

        marker.SprayHitReceived += HandleSprayHitReceived;
        marker.StrokeInterrupted += HandleStrokeInterrupted;
    }

    private void OnDisable()
    {
        if (marker != null)
        {
            marker.SprayHitReceived -= HandleSprayHitReceived;
            marker.StrokeInterrupted -= HandleStrokeInterrupted;
        }

        hasPreviousStrokeUV = false;
    }

    private void OnDestroy()
    {
        ReleaseRenderTexture(ref currentPaintTexture);
        ReleaseRenderTexture(ref workingPaintTexture);

        if (brushMaterial != null)
            Destroy(brushMaterial);
    }

    [ContextMenu("Clear Paint")]
    public void ClearPaint()
    {
        if (!IsInitialized)
            return;

        ClearRenderTexture(currentPaintTexture, initialClearColor);
        ClearRenderTexture(workingPaintTexture, initialClearColor);
        hasPreviousStrokeUV = false;
        ApplyPaintTextureToRenderer();
        PaintCleared?.Invoke();
    }

    public void SetPaintingEnabled(bool isEnabled)
    {
        paintingEnabled = isEnabled;

        if (!isEnabled)
            hasPreviousStrokeUV = false;
    }

    private void HandleSprayHitReceived(PaintSprayHit hit)
    {
        if (!paintingEnabled || !IsInitialized || hit.Paint == null)
            return;

        Vector2 uv = hit.TextureCoordinate;
        float opacity = Mathf.Clamp01(brushOpacity * hit.PaintAmount * paintAmountMultiplier);

        if (opacity <= 0f)
            return;

        if (hasPreviousStrokeUV)
        {
            float spacing = Mathf.Max(0.0001f, brushRadiusUV * 0.5f);
            float distance = Vector2.Distance(previousStrokeUV, uv);

            if (distance > spacing)
            {
                int requestedSteps = Mathf.CeilToInt(distance / spacing);
                int steps = Mathf.Min(requestedSteps, maximumInterpolatedStampsPerHit);

                for (int i = 1; i <= steps; i++)
                {
                    float t = (float)i / steps;
                    StampAt(Vector2.Lerp(previousStrokeUV, uv, t), hit.Paint, opacity);
                }
            }
            else
            {
                StampAt(uv, hit.Paint, opacity);
            }
        }
        else
        {
            StampAt(uv, hit.Paint, opacity);
        }

        previousStrokeUV = uv;
        hasPreviousStrokeUV = true;
    }

    private void HandleStrokeInterrupted()
    {
        hasPreviousStrokeUV = false;
    }

    private void StampAt(Vector2 uv, PaintColorDefinition paint, float opacity)
    {
        Color color = paint.DisplayColor;

        brushMaterial.SetVector(BrushUVId, new Vector4(uv.x, uv.y, 0f, 0f));
        brushMaterial.SetFloat(BrushRadiusId, brushRadiusUV);
        brushMaterial.SetFloat(BrushHardnessId, brushHardness);
        brushMaterial.SetColor(BrushColorId, color);
        brushMaterial.SetFloat(BrushOpacityId, opacity);

        Graphics.Blit(currentPaintTexture, workingPaintTexture, brushMaterial);

        RenderTexture previous = currentPaintTexture;
        currentPaintTexture = workingPaintTexture;
        workingPaintTexture = previous;

        ApplyPaintTextureToRenderer();

        StampApplied?.Invoke(new PaintStampData(
            paint,
            uv,
            brushRadiusUV,
            brushHardness,
            opacity));
    }

    private void ApplyPaintTextureToRenderer()
    {
        if (surfaceRenderer == null || propertyBlock == null)
            return;

        surfaceRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetTexture(PaintTexPropertyId, currentPaintTexture);
        surfaceRenderer.SetPropertyBlock(propertyBlock);
    }

    private RenderTexture CreatePaintRenderTexture()
    {
        RenderTexture renderTexture = new RenderTexture(
            textureResolution,
            textureResolution,
            0,
            RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false,
            enableRandomWrite = false,
            hideFlags = HideFlags.HideAndDontSave
        };

        renderTexture.Create();
        return renderTexture;
    }

    private static void ClearRenderTexture(RenderTexture renderTexture, Color color)
    {
        if (renderTexture == null)
            return;

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(false, true, color);
        RenderTexture.active = previous;
    }

    private static void ReleaseRenderTexture(ref RenderTexture renderTexture)
    {
        if (renderTexture == null)
            return;

        renderTexture.Release();
        Destroy(renderTexture);
        renderTexture = null;
    }

    private void OnValidate()
    {
        textureResolution = Mathf.Clamp(textureResolution, 64, 2048);
        brushRadiusUV = Mathf.Max(0.0001f, brushRadiusUV);
        paintAmountMultiplier = Mathf.Max(0f, paintAmountMultiplier);
        maximumInterpolatedStampsPerHit = Mathf.Clamp(maximumInterpolatedStampsPerHit, 1, 64);
    }
}
