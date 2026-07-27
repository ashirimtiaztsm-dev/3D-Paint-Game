using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Persistent HUD readout of the gun's current paint. Purely event-driven (no Update polling) —
// subscribes to PaintGunReservoir and refreshes only when colour or amount actually changes.
public class PaintReservoirUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PaintGunReservoir reservoir;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountLabel;

    [Header("Empty State")]
    [SerializeField] private Color emptyColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);

    private void Awake()
    {
        if (reservoir == null)
            Debug.LogWarning($"{nameof(PaintReservoirUI)} on '{name}' has no {nameof(reservoir)} assigned.", this);

        Refresh();
    }

    private void OnEnable()
    {
        if (reservoir == null)
            return;

        reservoir.PaintColorChanged += HandlePaintColorChanged;
        reservoir.AmountChanged += HandleAmountChanged;
    }

    private void OnDisable()
    {
        if (reservoir == null)
            return;

        reservoir.PaintColorChanged -= HandlePaintColorChanged;
        reservoir.AmountChanged -= HandleAmountChanged;
    }

    private void HandlePaintColorChanged(PaintColorDefinition paint)
    {
        ApplyColor(paint);
    }

    private void HandleAmountChanged(float currentAmount)
    {
        ApplyAmount(currentAmount);
    }

    private void Refresh()
    {
        if (reservoir == null)
            return;

        ApplyColor(reservoir.CurrentPaint);
        ApplyAmount(reservoir.CurrentAmount);
    }

    private void ApplyColor(PaintColorDefinition paint)
    {
        Color color = paint != null ? paint.DisplayColor : emptyColor;

        if (fillImage != null)
            fillImage.color = color;

        if (iconImage != null)
            iconImage.color = color;
    }

    private void ApplyAmount(float currentAmount)
    {
        if (reservoir == null)
            return;

        if (fillImage != null)
            fillImage.fillAmount = reservoir.NormalizedAmount;

        if (amountLabel != null)
            amountLabel.text = $"{Mathf.RoundToInt(currentAmount)} / {Mathf.RoundToInt(reservoir.MaximumCapacity)}";
    }
}
