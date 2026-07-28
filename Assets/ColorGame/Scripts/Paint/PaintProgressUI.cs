using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Event-driven progress display for the currently configured target: one overall bar plus one
// reusable row per required region. Never polls PaintCoverageTracker — every visual update is
// driven by its OverallProgressChanged / RegionProgressChanged / Completed events.
public class PaintProgressUI : MonoBehaviour
{
    [Header("Tracker")]
    [SerializeField] private PaintCoverageTracker tracker;

    [Header("Overall")]
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI percentageLabel;
    [SerializeField] private Image targetPreview;

    [Header("Region Rows")]
    [SerializeField] private RectTransform regionListContainer;
    [SerializeField] private PaintRegionProgressRow regionRowTemplate;

    private PaintRegionProgressRow[] regionRows;
    private bool rowsBuilt;

    private void Awake()
    {
        if (tracker == null)
            Debug.LogWarning($"{nameof(PaintProgressUI)} on '{name}' has no {nameof(tracker)} assigned.", this);

        if (regionRowTemplate != null)
            regionRowTemplate.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (tracker == null)
            return;

        tracker.OverallProgressChanged += HandleOverallProgressChanged;
        tracker.RegionProgressChanged += HandleRegionProgressChanged;
        tracker.Completed += HandleCompleted;

        BuildRegionRowsIfNeeded();
        Refresh();
    }

    private void OnDisable()
    {
        if (tracker != null)
        {
            tracker.OverallProgressChanged -= HandleOverallProgressChanged;
            tracker.RegionProgressChanged -= HandleRegionProgressChanged;
            tracker.Completed -= HandleCompleted;
        }
    }

    // Rows are created once per target (one per configured region) and reused for the rest of the
    // attempt. Region count does not change mid-level in this stage, so no rebuild path is needed yet.
    private void BuildRegionRowsIfNeeded()
    {
        if (rowsBuilt || tracker.TargetDefinition == null || regionRowTemplate == null || regionListContainer == null)
            return;

        var regions = tracker.TargetDefinition.Regions;
        regionRows = new PaintRegionProgressRow[regions.Count];

        for (int i = 0; i < regions.Count; i++)
        {
            PaintTargetDefinition.Region region = regions[i];

            PaintRegionProgressRow row = Instantiate(regionRowTemplate, regionListContainer);
            row.gameObject.SetActive(true);

            string label = string.IsNullOrEmpty(region.DisplayName)
                ? (region.RequiredPaint != null ? region.RequiredPaint.DisplayName : $"Region {i}")
                : region.DisplayName;

            row.Initialize(label, region.EffectiveUIColor);
            row.SetProgress(0f, false);

            regionRows[i] = row;
        }

        rowsBuilt = true;
    }

    private void Refresh()
    {
        HandleOverallProgressChanged(tracker.OverallProgress);

        if (targetPreview != null)
        {
            targetPreview.sprite = tracker.TargetDefinition != null ? tracker.TargetDefinition.PreviewSprite : null;
            targetPreview.enabled = targetPreview.sprite != null;
        }

        if (regionRows == null)
            return;

        for (int i = 0; i < regionRows.Length; i++)
        {
            if (regionRows[i] != null)
                regionRows[i].SetProgress(tracker.GetRegionProgress(i), tracker.IsRegionComplete(i));
        }
    }

    private void HandleOverallProgressChanged(float progress)
    {
        float clamped = Mathf.Clamp01(progress);

        if (progressFill != null)
            progressFill.fillAmount = clamped;

        if (percentageLabel != null)
            percentageLabel.text = $"{Mathf.RoundToInt(clamped * 100f)}%";
    }

    private void HandleRegionProgressChanged(int regionIndex, float progress)
    {
        if (regionRows == null || regionIndex < 0 || regionIndex >= regionRows.Length || regionRows[regionIndex] == null)
            return;

        regionRows[regionIndex].SetProgress(progress, tracker.IsRegionComplete(regionIndex));
    }

    private void HandleCompleted()
    {
        // Real gameplay state transition, win panel, and rewards arrive in a later stage. For now this
        // is deliberately just a debug signal that every required region has reached its threshold.
        Debug.Log($"TARGET COMPLETE: '{(tracker.TargetDefinition != null ? tracker.TargetDefinition.DisplayName : tracker.name)}'", this);
    }
}
