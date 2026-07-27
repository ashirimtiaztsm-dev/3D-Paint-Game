using System;
using UnityEngine;

// Tracks correct-colour coverage on a small CPU grid. It does not read the 512x512 GPU
// RenderTexture. Each actual brush stamp updates only the affected cells, making progress
// deterministic, overwrite-aware, and suitable for mobile devices.
public class PaintCoverageTracker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PaintableSurface paintableSurface;
    [SerializeField] private PaintTargetDefinition targetDefinition;

    [Header("Evaluation")]
    [SerializeField, Range(32, 256)] private int evaluationResolution = 128;
    [SerializeField, Range(0f, 1f)] private float paintedAlphaThreshold = 0.55f;
    [SerializeField, Range(0.01f, 1f)] private float colorTolerance = 0.28f;

    private PaintColorId[] requiredColorIds;
    private Color[] requiredColors;
    private Color[] paintedColors;
    private float[] paintedAlpha;

    private int totalRequiredCells;
    private int correctCells;
    private bool completionRaised;
    private bool initialized;
    private bool progressNotificationPending;

    public PaintTargetDefinition TargetDefinition => targetDefinition;
    public float CorrectProgress => totalRequiredCells > 0 ? (float)correctCells / totalRequiredCells : 0f;
    public bool IsComplete => initialized
        && targetDefinition != null
        && CorrectProgress >= targetDefinition.RequiredCompletion;

    public event Action<float> ProgressChanged;
    public event Action Completed;

    private void Awake()
    {
        if (paintableSurface == null)
            Debug.LogWarning($"{nameof(PaintCoverageTracker)} on '{name}' has no {nameof(paintableSurface)} assigned.", this);

        if (targetDefinition == null)
            Debug.LogWarning($"{nameof(PaintCoverageTracker)} on '{name}' has no {nameof(targetDefinition)} assigned.", this);
    }

    private void OnEnable()
    {
        if (paintableSurface == null)
            return;

        paintableSurface.StampApplied += HandleStampApplied;
        paintableSurface.PaintCleared += HandlePaintCleared;
    }

    private void Start()
    {
        InitializeEvaluationGrid();
    }

    private void OnDisable()
    {
        if (paintableSurface != null)
        {
            paintableSurface.StampApplied -= HandleStampApplied;
            paintableSurface.PaintCleared -= HandlePaintCleared;
        }

        progressNotificationPending = false;
    }

    private void LateUpdate()
    {
        if (!progressNotificationPending)
            return;

        progressNotificationPending = false;
        ProgressChanged?.Invoke(CorrectProgress);
    }

    [ContextMenu("Rebuild Target Grid")]
    public void InitializeEvaluationGrid()
    {
        initialized = false;
        completionRaised = false;
        totalRequiredCells = 0;
        correctCells = 0;

        if (targetDefinition == null || targetDefinition.Regions == null || targetDefinition.Regions.Count == 0)
        {
            Debug.LogWarning($"{nameof(PaintCoverageTracker)} on '{name}' has no configured target regions.", this);
            return;
        }

        int cellCount = evaluationResolution * evaluationResolution;
        requiredColorIds = new PaintColorId[cellCount];
        requiredColors = new Color[cellCount];
        paintedColors = new Color[cellCount];
        paintedAlpha = new float[cellCount];

        int overlappingCells = 0;

        for (int regionIndex = 0; regionIndex < targetDefinition.Regions.Count; regionIndex++)
        {
            PaintTargetDefinition.Region region = targetDefinition.Regions[regionIndex];
            if (region == null || region.RequiredPaint == null || region.MaskTexture == null)
                continue;

            Texture2D mask = region.MaskTexture;
            if (!mask.isReadable)
            {
                Debug.LogError(
                    $"Target mask '{mask.name}' must have Read/Write Enabled in its import settings.",
                    mask);
                continue;
            }

            Color32[] pixels = mask.GetPixels32();
            int maskWidth = mask.width;
            int maskHeight = mask.height;

            for (int y = 0; y < evaluationResolution; y++)
            {
                float v = (y + 0.5f) / evaluationResolution;
                int sourceY = Mathf.Clamp(Mathf.FloorToInt(v * maskHeight), 0, maskHeight - 1);

                for (int x = 0; x < evaluationResolution; x++)
                {
                    float u = (x + 0.5f) / evaluationResolution;
                    int sourceX = Mathf.Clamp(Mathf.FloorToInt(u * maskWidth), 0, maskWidth - 1);
                    Color32 maskPixel = pixels[sourceY * maskWidth + sourceX];

                    float maskValue = maskPixel.a < 255
                        ? maskPixel.a / 255f
                        : (maskPixel.r + maskPixel.g + maskPixel.b) / (3f * 255f);

                    if (maskValue < region.MaskThreshold)
                        continue;

                    int index = y * evaluationResolution + x;
                    if (requiredColorIds[index] != PaintColorId.None)
                        overlappingCells++;

                    requiredColorIds[index] = region.RequiredPaint.ColorId;
                    requiredColors[index] = region.RequiredPaint.DisplayColor;
                }
            }
        }

        for (int i = 0; i < requiredColorIds.Length; i++)
        {
            if (requiredColorIds[i] != PaintColorId.None)
                totalRequiredCells++;
        }

        if (overlappingCells > 0)
        {
            Debug.LogWarning(
                $"{nameof(PaintCoverageTracker)} on '{name}' found {overlappingCells} overlapping target-mask cells. Later regions take priority.",
                this);
        }

        if (totalRequiredCells == 0)
        {
            Debug.LogError(
                $"{nameof(PaintCoverageTracker)} on '{name}' produced an empty target grid. Check mask readability and thresholds.",
                this);
            return;
        }

        initialized = true;
        progressNotificationPending = false;
        ProgressChanged?.Invoke(0f);
    }

    public void ResetProgress()
    {
        if (!initialized)
            return;

        Array.Clear(paintedColors, 0, paintedColors.Length);
        Array.Clear(paintedAlpha, 0, paintedAlpha.Length);
        correctCells = 0;
        completionRaised = false;
        progressNotificationPending = false;
        ProgressChanged?.Invoke(0f);
    }

    private void HandlePaintCleared()
    {
        ResetProgress();
    }

    private void HandleStampApplied(PaintStampData stamp)
    {
        if (!initialized || stamp.Paint == null || stamp.Opacity <= 0f || stamp.RadiusUV <= 0f)
            return;

        ApplyStampToEvaluationGrid(stamp);

        float progress = CorrectProgress;
        progressNotificationPending = true;

        if (!completionRaised && progress >= targetDefinition.RequiredCompletion)
        {
            completionRaised = true;
            Completed?.Invoke();
        }
    }

    private void ApplyStampToEvaluationGrid(PaintStampData stamp)
    {
        Vector2 center = stamp.TextureCoordinate;
        float radius = stamp.RadiusUV;
        float innerRadius = radius * Mathf.Clamp01(stamp.Hardness);

        int minX = Mathf.Max(0, Mathf.FloorToInt((center.x - radius) * evaluationResolution));
        int maxX = Mathf.Min(evaluationResolution - 1, Mathf.CeilToInt((center.x + radius) * evaluationResolution));
        int minY = Mathf.Max(0, Mathf.FloorToInt((center.y - radius) * evaluationResolution));
        int maxY = Mathf.Min(evaluationResolution - 1, Mathf.CeilToInt((center.y + radius) * evaluationResolution));

        Color incomingColor = stamp.Paint.DisplayColor;

        for (int y = minY; y <= maxY; y++)
        {
            float v = (y + 0.5f) / evaluationResolution;

            for (int x = minX; x <= maxX; x++)
            {
                float u = (x + 0.5f) / evaluationResolution;
                float distance = Vector2.Distance(new Vector2(u, v), center);

                if (distance > radius)
                    continue;

                float brushMask = CalculateBrushMask(distance, innerRadius, radius);
                float stampAlpha = Mathf.Clamp01(brushMask * stamp.Opacity);
                if (stampAlpha <= 0f)
                    continue;

                int index = y * evaluationResolution + x;
                bool wasCorrect = IsCellCorrect(index);

                paintedColors[index] = Color.Lerp(paintedColors[index], incomingColor, stampAlpha);
                paintedAlpha[index] = Mathf.Clamp01(
                    paintedAlpha[index] + stampAlpha * (1f - paintedAlpha[index]));

                bool isCorrectNow = IsCellCorrect(index);

                if (wasCorrect == isCorrectNow)
                    continue;

                correctCells += isCorrectNow ? 1 : -1;
            }
        }
    }

    private bool IsCellCorrect(int index)
    {
        if (requiredColorIds[index] == PaintColorId.None)
            return false;

        if (paintedAlpha[index] < paintedAlphaThreshold)
            return false;

        Color painted = paintedColors[index];
        Color required = requiredColors[index];

        float red = painted.r - required.r;
        float green = painted.g - required.g;
        float blue = painted.b - required.b;
        float distance = Mathf.Sqrt(red * red + green * green + blue * blue);

        return distance <= colorTolerance;
    }

    private static float CalculateBrushMask(float distance, float innerRadius, float outerRadius)
    {
        if (distance <= innerRadius)
            return 1f;

        float featherWidth = Mathf.Max(0.00001f, outerRadius - innerRadius);
        float t = Mathf.Clamp01((distance - innerRadius) / featherWidth);
        float smooth = t * t * (3f - 2f * t);
        return 1f - smooth;
    }

    private void OnValidate()
    {
        evaluationResolution = Mathf.Clamp(evaluationResolution, 32, 256);
        paintedAlphaThreshold = Mathf.Clamp01(paintedAlphaThreshold);
        colorTolerance = Mathf.Clamp(colorTolerance, 0.01f, 1f);
    }
}
