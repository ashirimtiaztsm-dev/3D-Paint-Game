using System;
using UnityEngine;

// Builds one combined "allowed to paint here" mask per PaintColorId used by the active
// PaintTargetDefinition, so PaintableSurface can clip the GPU brush stamp to exactly the regions
// that require that colour. Masks are built once (on Awake / target change) and never touched while
// spraying. Holds no gameplay-progress logic — PaintCoverageTracker owns that independently.
public class PaintTargetMaskProvider : MonoBehaviour
{
    [SerializeField] private PaintTargetDefinition targetDefinition;

    [Header("Runtime Mask")]
    [SerializeField] private FilterMode maskFilterMode = FilterMode.Bilinear;

    private static readonly int ColorIdCount = Enum.GetValues(typeof(PaintColorId)).Length;

    private Texture2D[] allowedMasksByColorId;

    public PaintTargetDefinition TargetDefinition => targetDefinition;

    public event Action MasksRebuilt;

    private void Awake()
    {
        allowedMasksByColorId = new Texture2D[ColorIdCount];
        RebuildMasks();
    }

    private void OnDestroy()
    {
        ReleaseAllMasks();
    }

    public Texture GetAllowedMask(PaintColorId colorId)
    {
        if (colorId == PaintColorId.None || allowedMasksByColorId == null)
            return null;

        int index = (int)colorId;
        return index >= 0 && index < allowedMasksByColorId.Length ? allowedMasksByColorId[index] : null;
    }

    public bool HasAllowedRegion(PaintColorId colorId)
    {
        return GetAllowedMask(colorId) != null;
    }

    public void SetTargetDefinition(PaintTargetDefinition definition)
    {
        targetDefinition = definition;
        RebuildMasks();
    }

    [ContextMenu("Rebuild Masks")]
    public void RebuildMasks()
    {
        ReleaseAllMasks();

        if (allowedMasksByColorId == null)
            allowedMasksByColorId = new Texture2D[ColorIdCount];

        if (targetDefinition == null || targetDefinition.Regions == null || targetDefinition.Regions.Count == 0)
        {
            Debug.LogWarning($"{nameof(PaintTargetMaskProvider)} on '{name}' has no configured target — no colour will be paintable.", this);
            MasksRebuilt?.Invoke();
            return;
        }

        var regions = targetDefinition.Regions;

        // One accumulation buffer per used colour id, built lazily the first time that colour is seen.
        var accumulated = new float[ColorIdCount][];
        var referenceWidth = new int[ColorIdCount];
        var referenceHeight = new int[ColorIdCount];

        for (int regionIndex = 0; regionIndex < regions.Count; regionIndex++)
        {
            PaintTargetDefinition.Region region = regions[regionIndex];

            if (region == null || !region.IsValidConfiguration)
                continue;

            Texture2D mask = region.MaskTexture;

            if (!mask.isReadable)
            {
                Debug.LogError(
                    $"{nameof(PaintTargetMaskProvider)} on '{name}': mask '{mask.name}' (region {regionIndex} of '{targetDefinition.name}') must have Read/Write Enabled — skipping this region for visual clipping.",
                    mask);
                continue;
            }

            int colorIndex = (int)region.RequiredPaint.ColorId;

            if (accumulated[colorIndex] == null)
            {
                referenceWidth[colorIndex] = mask.width;
                referenceHeight[colorIndex] = mask.height;
                accumulated[colorIndex] = new float[mask.width * mask.height];
            }
            else if (mask.width != referenceWidth[colorIndex] || mask.height != referenceHeight[colorIndex])
            {
                Debug.LogError(
                    $"{nameof(PaintTargetMaskProvider)} on '{name}': mask '{mask.name}' (region {regionIndex} of '{targetDefinition.name}') is {mask.width}x{mask.height} but other regions requiring the same colour use {referenceWidth[colorIndex]}x{referenceHeight[colorIndex]} — skipping this region for visual clipping.",
                    mask);
                continue;
            }

            Color32[] pixels = mask.GetPixels32();
            float[] buffer = accumulated[colorIndex];
            float threshold = region.MaskThreshold;

            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 p = pixels[i];
                float maskValue = p.a < 255
                    ? p.a / 255f
                    : (p.r + p.g + p.b) / (3f * 255f);

                float included = maskValue >= threshold ? 1f : 0f;

                if (included > buffer[i])
                    buffer[i] = included;
            }
        }

        int builtCount = 0;

        for (int colorIndex = 0; colorIndex < ColorIdCount; colorIndex++)
        {
            if (accumulated[colorIndex] == null)
                continue;

            Texture2D combined = BuildMaskTexture(accumulated[colorIndex], referenceWidth[colorIndex], referenceHeight[colorIndex]);

            if (combined == null)
            {
                Debug.LogWarning(
                    $"{nameof(PaintTargetMaskProvider)} on '{name}': combined mask for colour '{(PaintColorId)colorIndex}' produced zero valid pixels — that colour will not be paintable.",
                    this);
                continue;
            }

            allowedMasksByColorId[colorIndex] = combined;
            builtCount++;
        }

        if (builtCount == 0)
        {
            Debug.LogWarning(
                $"{nameof(PaintTargetMaskProvider)} on '{name}': target '{targetDefinition.name}' produced no usable allowed masks — no colour will be paintable.",
                this);
        }

        MasksRebuilt?.Invoke();
    }

    private Texture2D BuildMaskTexture(float[] values, int width, int height)
    {
        bool anyIncluded = false;

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] > 0f)
            {
                anyIncluded = true;
                break;
            }
        }

        if (!anyIncluded)
            return null;

        bool useR8 = SystemInfo.SupportsTextureFormat(TextureFormat.R8);
        TextureFormat format = useR8 ? TextureFormat.R8 : TextureFormat.RGBA32;

        var texture = new Texture2D(width, height, format, false, true)
        {
            filterMode = maskFilterMode,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        if (useR8)
        {
            var bytes = new byte[values.Length];
            for (int i = 0; i < values.Length; i++)
                bytes[i] = values[i] > 0f ? (byte)255 : (byte)0;

            texture.SetPixelData(bytes, 0);
        }
        else
        {
            var colors = new Color32[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                byte v = values[i] > 0f ? (byte)255 : (byte)0;
                colors[i] = new Color32(v, v, v, v);
            }

            texture.SetPixels32(colors);
        }

        texture.Apply(false, true);
        return texture;
    }

    private void ReleaseAllMasks()
    {
        if (allowedMasksByColorId == null)
            return;

        for (int i = 0; i < allowedMasksByColorId.Length; i++)
        {
            if (allowedMasksByColorId[i] == null)
                continue;

            // RebuildMasks() can legitimately run in the Editor outside Play mode (ContextMenu entry,
            // or SetTargetDefinition called while authoring) — Destroy() only works during Play mode.
            if (Application.isPlaying)
                Destroy(allowedMasksByColorId[i]);
            else
                DestroyImmediate(allowedMasksByColorId[i]);

            allowedMasksByColorId[i] = null;
        }
    }
}
