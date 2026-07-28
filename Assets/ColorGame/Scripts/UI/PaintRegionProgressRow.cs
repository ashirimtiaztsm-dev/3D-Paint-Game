using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One reusable row in the region-progress list. PaintProgressUI instantiates one of these per target
// region when the level loads and reuses it for the rest of the attempt — never rebuilt per stamp.
public class PaintRegionProgressRow : MonoBehaviour
{
    [SerializeField] private Image colorSwatch;
    [SerializeField] private TextMeshProUGUI regionLabel;
    [SerializeField] private Image progressFill;
    [SerializeField] private TextMeshProUGUI percentageLabel;

    public void Initialize(string displayName, Color swatchColor)
    {
        if (regionLabel != null)
            regionLabel.text = string.IsNullOrEmpty(displayName) ? "Region" : displayName;

        if (colorSwatch != null)
            colorSwatch.color = swatchColor;
    }

    public void SetProgress(float progress, bool isComplete)
    {
        float clamped = Mathf.Clamp01(progress);

        if (progressFill != null)
            progressFill.fillAmount = clamped;

        if (percentageLabel != null)
            percentageLabel.text = isComplete ? "DONE" : $"{Mathf.RoundToInt(clamped * 100f)}%";
    }
}
