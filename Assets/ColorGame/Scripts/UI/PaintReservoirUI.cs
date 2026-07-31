using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Event-driven HUD for the paint currently stored in the gun.
// The grey background remains visible while the colored fill rises
// from bottom to top according to the reservoir's normalized amount.
public class PaintReservoirUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PaintGunReservoir reservoir;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountLabel;

    [Header("Colors")]
    [SerializeField] private Color emptyColor =
        new Color(0.35f, 0.35f, 0.35f, 0.9f);

    [Header("Settings")]
    [SerializeField, Min(0f)] private float emptyEpsilon = 0.001f;

    private void Awake()
    {
        ValidateReferences();
        ConfigureFillImage();
        RefreshVisuals();
    }

    private void OnEnable()
    {
        if (reservoir != null)
        {
            reservoir.PaintColorChanged += HandlePaintColorChanged;
            reservoir.AmountChanged += HandleAmountChanged;
        }

        ConfigureFillImage();
        RefreshVisuals();
    }

    private void OnDisable()
    {
        if (reservoir != null)
        {
            reservoir.PaintColorChanged -= HandlePaintColorChanged;
            reservoir.AmountChanged -= HandleAmountChanged;
        }
    }

    private void HandlePaintColorChanged(PaintColorDefinition paint)
    {
        RefreshVisuals();
    }

    private void HandleAmountChanged(float currentAmount)
    {
        RefreshVisuals();
    }

    private void ConfigureFillImage()
    {
        if (fillImage == null)
            return;

        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Vertical;
        fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        fillImage.fillClockwise = true;
        fillImage.preserveAspect = false;
        fillImage.raycastTarget = false;
    }

    private void RefreshVisuals()
    {
        if (reservoir == null)
        {
            ApplyEmptyState();
            return;
        }

        float normalizedAmount =
            Mathf.Clamp01(reservoir.NormalizedAmount);

        bool hasVisiblePaint =
            normalizedAmount > emptyEpsilon &&
            reservoir.CurrentPaint != null;

        if (backgroundImage != null)
        {
            backgroundImage.color = emptyColor;
            backgroundImage.enabled = true;
        }

        if (fillImage != null)
        {
            fillImage.fillAmount = normalizedAmount;
            fillImage.color = hasVisiblePaint
                ? reservoir.CurrentPaint.DisplayColor
                : emptyColor;

            // At zero amount, hide only the colored layer.
            // The grey background remains visible as the empty bar.
            fillImage.enabled = hasVisiblePaint;
        }

        if (iconImage != null)
        {
            iconImage.color = hasVisiblePaint
                ? reservoir.CurrentPaint.DisplayColor
                : emptyColor;
        }

        if (amountLabel != null)
        {
            amountLabel.text =
                $"{Mathf.RoundToInt(reservoir.CurrentAmount)} / " +
                $"{Mathf.RoundToInt(reservoir.MaximumCapacity)}";
        }
    }

    private void ApplyEmptyState()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = emptyColor;
            backgroundImage.enabled = true;
        }

        if (fillImage != null)
        {
            fillImage.fillAmount = 0f;
            fillImage.color = emptyColor;
            fillImage.enabled = false;
        }

        if (iconImage != null)
            iconImage.color = emptyColor;

        if (amountLabel != null)
            amountLabel.text = "0 / 0";
    }

    private void ValidateReferences()
    {
        if (reservoir == null)
            Debug.LogWarning(
                $"{nameof(PaintReservoirUI)} on '{name}' has no reservoir assigned.",
                this);

        if (backgroundImage == null)
            Debug.LogWarning(
                $"{nameof(PaintReservoirUI)} on '{name}' has no background image assigned.",
                this);

        if (fillImage == null)
            Debug.LogWarning(
                $"{nameof(PaintReservoirUI)} on '{name}' has no fill image assigned.",
                this);

        if (amountLabel == null)
            Debug.LogWarning(
                $"{nameof(PaintReservoirUI)} on '{name}' has no amount label assigned.",
                this);
    }

    private void OnValidate()
    {
        emptyEpsilon = Mathf.Max(0f, emptyEpsilon);
    }
}
