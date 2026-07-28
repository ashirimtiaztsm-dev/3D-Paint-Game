// Lightweight snapshot of one target region's runtime progress, handed out by PaintCoverageTracker.
// Carries no MonoBehaviour/UnityEngine.Object references so it can be freely copied and read from UI.
public readonly struct PaintRegionProgress
{
    public PaintRegionProgress(
        int regionIndex,
        PaintColorDefinition requiredPaint,
        string displayName,
        float progress,
        float requiredCompletion,
        bool isComplete)
    {
        RegionIndex = regionIndex;
        RequiredPaint = requiredPaint;
        DisplayName = displayName;
        Progress = progress;
        RequiredCompletion = requiredCompletion;
        IsComplete = isComplete;
    }

    public int RegionIndex { get; }
    public PaintColorDefinition RequiredPaint { get; }
    public string DisplayName { get; }
    public float Progress { get; }
    public float RequiredCompletion { get; }
    public bool IsComplete { get; }
}
