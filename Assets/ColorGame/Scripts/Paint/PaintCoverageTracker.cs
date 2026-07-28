using System;
using UnityEngine;

// Tracks correct-colour coverage on a small CPU grid, per target region. Never reads the 512x512 GPU
// RenderTexture — each actual brush stamp updates only the affected cells via PaintableSurface.StampApplied,
// keeping progress deterministic, overwrite-aware, and mobile-friendly.
//
// Cell-ownership policy: when regions overlap on the same evaluation cell, the FIRST configured region
// (lowest index in PaintTargetDefinition.Regions) owns that cell. Later regions never take ownership away
// from an already-claimed cell — this is deliberate and documented, not an oversight.
public class PaintCoverageTracker : MonoBehaviour
{
    private const int NoRegion = -1;

    [Header("References")]
    [SerializeField] private PaintableSurface paintableSurface;
    [SerializeField] private PaintTargetDefinition targetDefinition;

    [Header("Evaluation")]
    [SerializeField, Range(32, 256)] private int evaluationResolution = 128;
    [SerializeField, Range(0f, 1f)] private float paintedAlphaThreshold = 0.55f;
    [SerializeField, Range(0.01f, 1f)] private float colorTolerance = 0.28f;

    // Per-cell grid (size = evaluationResolution^2)
    private int[] cellRegionIndex;
    private PaintColorId[] cellRequiredColorId;
    private Color[] cellRequiredColor;
    private Color[] paintedColors;
    private float[] paintedAlpha;

    // Per-region runtime state (size = targetDefinition.Regions.Count)
    private bool[] regionValid;
    private int[] regionTotalCells;
    private int[] regionCorrectCells;
    private float[] regionRequiredCompletion;
    private bool[] regionMeetsThreshold;
    private bool[] regionCompletedEventLatch;
    private bool[] regionProgressDirty;

    private int regionCount;
    private int totalCorrectCellsOverall;
    private int totalRequiredCellsOverall;
    private bool overallProgressDirty;
    private bool overallCompletionRaised;
    private bool initialized;

    public PaintTargetDefinition TargetDefinition => targetDefinition;
    public int RegionCount => regionCount;

    public float OverallProgress => totalRequiredCellsOverall > 0
        ? (float)totalCorrectCellsOverall / totalRequiredCellsOverall
        : 0f;

    public bool IsComplete
    {
        get
        {
            if (!initialized)
                return false;

            bool hasValidRegion = false;

            for (int i = 0; i < regionCount; i++)
            {
                if (!regionValid[i])
                    continue;

                hasValidRegion = true;

                if (!regionMeetsThreshold[i])
                    return false;
            }

            return hasValidRegion;
        }
    }

    public event Action<float> OverallProgressChanged;
    public event Action<int, float> RegionProgressChanged;
    public event Action<int> RegionCompleted;
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

        overallProgressDirty = false;

        for (int i = 0; i < regionCount; i++)
            regionProgressDirty[i] = false;
    }

    private void LateUpdate()
    {
        FlushPendingNotifications();
    }

    public float GetRegionProgress(int regionIndex)
    {
        if (!IsValidRegionIndex(regionIndex) || regionTotalCells[regionIndex] <= 0)
            return 0f;

        return (float)regionCorrectCells[regionIndex] / regionTotalCells[regionIndex];
    }

    public bool IsRegionComplete(int regionIndex)
    {
        return IsValidRegionIndex(regionIndex) && regionMeetsThreshold[regionIndex];
    }

    public PaintRegionProgress GetRegionProgressData(int regionIndex)
    {
        if (!IsValidRegionIndex(regionIndex))
            return new PaintRegionProgress(regionIndex, null, string.Empty, 0f, 0f, false);

        PaintTargetDefinition.Region region = targetDefinition.Regions[regionIndex];

        return new PaintRegionProgress(
            regionIndex,
            region.RequiredPaint,
            region.DisplayName,
            GetRegionProgress(regionIndex),
            regionRequiredCompletion[regionIndex],
            regionMeetsThreshold[regionIndex]);
    }

    [ContextMenu("Rebuild Target Grid")]
    public void InitializeEvaluationGrid()
    {
        initialized = false;
        overallCompletionRaised = false;
        overallProgressDirty = false;
        totalCorrectCellsOverall = 0;
        totalRequiredCellsOverall = 0;
        regionCount = 0;

        if (targetDefinition == null || targetDefinition.Regions == null || targetDefinition.Regions.Count == 0)
        {
            Debug.LogWarning($"{nameof(PaintCoverageTracker)} on '{name}' has no configured target regions.", this);
            return;
        }

        regionCount = targetDefinition.Regions.Count;

        int cellCount = evaluationResolution * evaluationResolution;
        cellRegionIndex = new int[cellCount];
        cellRequiredColorId = new PaintColorId[cellCount];
        cellRequiredColor = new Color[cellCount];
        paintedColors = new Color[cellCount];
        paintedAlpha = new float[cellCount];

        for (int i = 0; i < cellCount; i++)
            cellRegionIndex[i] = NoRegion;

        regionValid = new bool[regionCount];
        regionTotalCells = new int[regionCount];
        regionCorrectCells = new int[regionCount];
        regionRequiredCompletion = new float[regionCount];
        regionMeetsThreshold = new bool[regionCount];
        regionCompletedEventLatch = new bool[regionCount];
        regionProgressDirty = new bool[regionCount];

        int overlappingCells = 0;

        for (int regionIndex = 0; regionIndex < regionCount; regionIndex++)
        {
            PaintTargetDefinition.Region region = targetDefinition.Regions[regionIndex];
            regionRequiredCompletion[regionIndex] = 0.95f;

            if (region == null || !region.IsValidConfiguration)
            {
                Debug.LogWarning(
                    $"{nameof(PaintCoverageTracker)} on '{name}': region {regionIndex} of target '{targetDefinition.name}' has invalid configuration (missing paint/mask or bad completion threshold) and will be skipped.",
                    this);
                continue;
            }

            regionRequiredCompletion[regionIndex] = region.RequiredCompletion;

            Texture2D mask = region.MaskTexture;
            if (!mask.isReadable)
            {
                Debug.LogError(
                    $"Target mask '{mask.name}' (region {regionIndex} of '{targetDefinition.name}') must have Read/Write Enabled in its import settings.",
                    mask);
                continue;
            }

            Color32[] pixels = mask.GetPixels32();
            int maskWidth = mask.width;
            int maskHeight = mask.height;
            int claimedCells = 0;

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

                    if (cellRegionIndex[index] != NoRegion)
                    {
                        // Already owned by an earlier region — first-region-owns policy, do not steal it.
                        overlappingCells++;
                        continue;
                    }

                    cellRegionIndex[index] = regionIndex;
                    cellRequiredColorId[index] = region.RequiredPaint.ColorId;
                    cellRequiredColor[index] = region.RequiredPaint.DisplayColor;
                    claimedCells++;
                }
            }

            if (claimedCells == 0)
            {
                Debug.LogWarning(
                    $"{nameof(PaintCoverageTracker)} on '{name}': region {regionIndex} ('{region.DisplayName}') of target '{targetDefinition.name}' produced zero required cells and will be skipped.",
                    this);
                continue;
            }

            regionValid[regionIndex] = true;
            regionTotalCells[regionIndex] = claimedCells;
            totalRequiredCellsOverall += claimedCells;
        }

        if (overlappingCells > 0)
        {
            Debug.LogWarning(
                $"{nameof(PaintCoverageTracker)} on '{name}': target '{targetDefinition.name}' has {overlappingCells} overlapping mask cells across regions. The first configured region owns each overlapping cell; later regions do not overwrite ownership.",
                this);
        }

        bool hasAnyValidRegion = false;
        for (int i = 0; i < regionCount; i++)
        {
            if (regionValid[i])
            {
                hasAnyValidRegion = true;
                break;
            }
        }

        if (!hasAnyValidRegion)
        {
            Debug.LogError(
                $"{nameof(PaintCoverageTracker)} on '{name}': target '{targetDefinition.name}' has zero valid regions. It can never complete.",
                this);
            return;
        }

        initialized = true;

        OverallProgressChanged?.Invoke(0f);

        for (int i = 0; i < regionCount; i++)
        {
            if (regionValid[i])
                RegionProgressChanged?.Invoke(i, 0f);
        }
    }

    public void ResetProgress()
    {
        if (!initialized)
            return;

        Array.Clear(paintedColors, 0, paintedColors.Length);
        Array.Clear(paintedAlpha, 0, paintedAlpha.Length);
        Array.Clear(regionCorrectCells, 0, regionCorrectCells.Length);
        Array.Clear(regionMeetsThreshold, 0, regionMeetsThreshold.Length);
        Array.Clear(regionCompletedEventLatch, 0, regionCompletedEventLatch.Length);
        Array.Clear(regionProgressDirty, 0, regionProgressDirty.Length);

        totalCorrectCellsOverall = 0;
        overallCompletionRaised = false;
        overallProgressDirty = false;

        OverallProgressChanged?.Invoke(0f);

        for (int i = 0; i < regionCount; i++)
        {
            if (regionValid[i])
                RegionProgressChanged?.Invoke(i, 0f);
        }
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
                int index = y * evaluationResolution + x;
                int regionIndex = cellRegionIndex[index];

                // Painting outside every configured region's mask never counts toward anything.
                if (regionIndex == NoRegion || !regionValid[regionIndex])
                    continue;

                // Wrong-color paint is rejected outright for this cell: no blend, no alpha change, no
                // progress change, and it can never overwrite paint already correct for this cell.
                if (cellRequiredColorId[index] != stamp.Paint.ColorId)
                    continue;

                float u = (x + 0.5f) / evaluationResolution;
                float distance = Vector2.Distance(new Vector2(u, v), center);

                if (distance > radius)
                    continue;

                float brushMask = CalculateBrushMask(distance, innerRadius, radius);
                float stampAlpha = Mathf.Clamp01(brushMask * stamp.Opacity);
                if (stampAlpha <= 0f)
                    continue;

                bool wasCorrect = IsCellCorrect(index, regionIndex);

                paintedColors[index] = Color.Lerp(paintedColors[index], incomingColor, stampAlpha);
                paintedAlpha[index] = Mathf.Clamp01(
                    paintedAlpha[index] + stampAlpha * (1f - paintedAlpha[index]));

                bool isCorrectNow = IsCellCorrect(index, regionIndex);

                if (wasCorrect == isCorrectNow)
                    continue;

                int delta = isCorrectNow ? 1 : -1;
                regionCorrectCells[regionIndex] += delta;
                totalCorrectCellsOverall += delta;

                regionProgressDirty[regionIndex] = true;
                overallProgressDirty = true;
            }
        }
    }

    private bool IsCellCorrect(int index, int regionIndex)
    {
        if (paintedAlpha[index] < paintedAlphaThreshold)
            return false;

        Color painted = paintedColors[index];
        Color required = cellRequiredColor[index];

        float red = painted.r - required.r;
        float green = painted.g - required.g;
        float blue = painted.b - required.b;
        float distance = Mathf.Sqrt(red * red + green * green + blue * blue);

        return distance <= colorTolerance;
    }

    private void FlushPendingNotifications()
    {
        for (int i = 0; i < regionCount; i++)
        {
            if (!regionProgressDirty[i])
                continue;

            regionProgressDirty[i] = false;

            float progress = GetRegionProgress(i);
            RegionProgressChanged?.Invoke(i, progress);

            bool meetsThreshold = progress >= regionRequiredCompletion[i];
            regionMeetsThreshold[i] = meetsThreshold;

            if (meetsThreshold && !regionCompletedEventLatch[i])
            {
                regionCompletedEventLatch[i] = true;
                RegionCompleted?.Invoke(i);
            }
        }

        if (overallProgressDirty)
        {
            overallProgressDirty = false;
            OverallProgressChanged?.Invoke(OverallProgress);
        }

        if (!overallCompletionRaised && IsComplete)
        {
            overallCompletionRaised = true;
            Completed?.Invoke();
        }
    }

    private bool IsValidRegionIndex(int regionIndex)
    {
        return initialized && regionIndex >= 0 && regionIndex < regionCount && regionValid[regionIndex];
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
