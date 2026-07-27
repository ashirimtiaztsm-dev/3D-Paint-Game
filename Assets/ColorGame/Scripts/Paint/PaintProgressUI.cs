using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Event-driven progress display for the currently configured target.
public class PaintProgressUI : MonoBehaviour
{
    [SerializeField] private PaintCoverageTracker tracker;
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI percentageLabel;
    [SerializeField] private Image targetPreview;

    private void Awake()
    {
        if (tracker == null)
            Debug.LogWarning($"{nameof(PaintProgressUI)} on '{name}' has no {nameof(tracker)} assigned.", this);
    }

    private void OnEnable()
    {
        if (tracker == null)
            return;

        tracker.ProgressChanged += HandleProgressChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (tracker != null)
            tracker.ProgressChanged -= HandleProgressChanged;
    }

    private void Refresh()
    {
        if (tracker == null)
            return;

        HandleProgressChanged(tracker.CorrectProgress);

        if (targetPreview != null && tracker.TargetDefinition != null)
        {
            targetPreview.sprite = tracker.TargetDefinition.PreviewSprite;
            targetPreview.enabled = tracker.TargetDefinition.PreviewSprite != null;
        }
    }

    private void HandleProgressChanged(float progress)
    {
        float clamped = Mathf.Clamp01(progress);

        if (progressFill != null)
            progressFill.fillAmount = clamped;

        if (percentageLabel != null)
            percentageLabel.text = $"{Mathf.RoundToInt(clamped * 100f)}%";
    }
}
