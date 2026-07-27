using UnityEngine;

// Permanent visual painting for one surface: stamps each accepted spray hit into a persistent,
// ping-ponged RenderTexture and composites it onto the surface via MaterialPropertyBlock. Holds no
// gameplay state (no colour correctness, no completion tracking) — purely "where has paint landed
// and what does it look like", left for a later stage to interpret.
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
    [SerializeField] private float brushRadiusUV = 0.045f;
    [SerializeField] [Range(0f, 1f)] private float brushHardness = 0.8f;
    [SerializeField] [Range(0f, 1f)] private float brushOpacity = 1f;
    [SerializeField] private float paintAmountMultiplier = 4f;

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
        if (marker != null)
        {
            marker.SprayHitReceived += HandleSprayHitReceived;
            marker.StrokeInterrupted += HandleStrokeInterrupted;
        }
    }

    private void OnDisable()
    {
        if (marker != null)
        {
            marker.SprayHitReceived -= HandleSprayHitReceived;
            marker.StrokeInterrupted -= HandleStrokeInterrupted;
        }
    }

    private void OnDestroy()
    {
        if (currentPaintTexture != null)
        {
            currentPaintTexture.Release();
            Destroy(currentPaintTexture);
        }

        if (workingPaintTexture != null)
        {
            workingPaintTexture.Release();
            Destroy(workingPaintTexture);
        }

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
        Color color = hit.Paint.DisplayColor;
        float opacity = Mathf.Clamp01(brushOpacity * hit.PaintAmount * paintAmountMultiplier);

        if (opacity <= 0f)
            return;

        if (hasPreviousStrokeUV)
        {
            float spacing = brushRadiusUV * 0.5f;
            float distance = Vector2.Distance(previousStrokeUV, uv);

            if (distance > spacing)
            {
                int steps = Mathf.CeilToInt(distance / spacing);

                for (int i = 1; i <= steps; i++)
                {
                    float t = (float)i / steps;
                    StampAt(Vector2.Lerp(previousStrokeUV, uv, t), color, opacity);
                }
            }
            else
            {
                StampAt(uv, color, opacity);
            }
        }
        else
        {
            StampAt(uv, color, opacity);
        }

        previousStrokeUV = uv;
        hasPreviousStrokeUV = true;
    }

    private void HandleStrokeInterrupted()
    {
        hasPreviousStrokeUV = false;
    }

    private void StampAt(Vector2 uv, Color color, float opacity)
    {
        brushMaterial.SetVector(BrushUVId, new Vector4(uv.x, uv.y, 0f, 0f));
        brushMaterial.SetFloat(BrushRadiusId, brushRadiusUV);
        brushMaterial.SetFloat(BrushHardnessId, brushHardness);
        brushMaterial.SetColor(BrushColorId, color);
        brushMaterial.SetFloat(BrushOpacityId, opacity);

        Graphics.Blit(currentPaintTexture, workingPaintTexture, brushMaterial);

        (currentPaintTexture, workingPaintTexture) = (workingPaintTexture, currentPaintTexture);

        ApplyPaintTextureToRenderer();
    }

    private void ApplyPaintTextureToRenderer()
    {
        if (surfaceRenderer == null)
            return;

        surfaceRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetTexture(PaintTexPropertyId, currentPaintTexture);
        surfaceRenderer.SetPropertyBlock(propertyBlock);
    }

    private RenderTexture CreatePaintRenderTexture()
    {
        var renderTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32)
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
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, color);
        RenderTexture.active = previous;
    }
}
