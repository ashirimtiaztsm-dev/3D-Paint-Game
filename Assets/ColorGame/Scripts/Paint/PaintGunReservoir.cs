using System;
using UnityEngine;

// Owns the player's paint-gun state: current colour, current amount, capacity. Contains the
// same-colour-tops-up / different-colour-replaces policy so it lives in one isolated place and can
// be changed later without touching PaintFillController or PaintTank.
public class PaintGunReservoir : MonoBehaviour
{
    [SerializeField] private float maximumCapacity = 100f;

    private PaintColorDefinition currentPaint;
    private float currentAmount;

    public PaintColorDefinition CurrentPaint => currentPaint;
    public float CurrentAmount => currentAmount;
    public float MaximumCapacity => maximumCapacity;
    public float NormalizedAmount => maximumCapacity > 0f ? currentAmount / maximumCapacity : 0f;
    public bool IsEmpty => currentAmount <= 0f;
    public bool IsFull => currentAmount >= maximumCapacity;

    public event Action<PaintColorDefinition> PaintColorChanged;
    public event Action<float> AmountChanged;

    // Returns the amount actually added (0 if nothing could be accepted). Filling from a different
    // colour than currently held clears the old amount and adopts the new colour, starting from
    // zero — but only on the first transfer that actually succeeds; a request that cannot accept any
    // paint (e.g. capacity is zero) never touches the existing colour/amount at all.
    public float AddPaint(PaintColorDefinition paint, float requestedAmount)
    {
        if (paint == null || requestedAmount <= 0f || maximumCapacity <= 0f)
            return 0f;

        bool isColorChange = currentPaint != paint;

        // Compute how much COULD be accepted before mutating any state, so a request that can't
        // actually fit never clears/replaces the existing colour.
        float availableSpace = isColorChange ? maximumCapacity : Mathf.Max(0f, maximumCapacity - currentAmount);
        float amountToAdd = Mathf.Min(requestedAmount, availableSpace);

        if (amountToAdd <= 0f)
            return 0f;

        if (isColorChange)
        {
            currentAmount = 0f;
            currentPaint = paint;
        }

        currentAmount = Mathf.Clamp(currentAmount + amountToAdd, 0f, maximumCapacity);

        if (isColorChange)
            PaintColorChanged?.Invoke(currentPaint);

        AmountChanged?.Invoke(currentAmount);

        return amountToAdd;
    }

    // Not used yet (reserved for the firing stage). Returns the amount actually consumed.
    public float ConsumePaint(float requestedAmount)
    {
        if (requestedAmount <= 0f)
            return 0f;

        float amountToConsume = Mathf.Min(requestedAmount, currentAmount);
        if (amountToConsume <= 0f)
            return 0f;

        currentAmount = Mathf.Clamp(currentAmount - amountToConsume, 0f, maximumCapacity);
        AmountChanged?.Invoke(currentAmount);

        return amountToConsume;
    }

    private void OnValidate()
    {
        maximumCapacity = Mathf.Max(0f, maximumCapacity);
    }

    public void Clear()
    {
        if (currentAmount <= 0f && currentPaint == null)
            return;

        currentAmount = 0f;
        currentPaint = null;

        PaintColorChanged?.Invoke(currentPaint);
        AmountChanged?.Invoke(currentAmount);
    }
}
