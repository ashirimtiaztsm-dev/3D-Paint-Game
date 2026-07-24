using UnityEngine;

// Keeps MoveJoystick and CameraLookArea sized to exactly the left/right halves of the current
// safe area, in real Canvas-local units (not hard-coded reference-resolution pixels), so the split
// stays correct across aspect ratios and CanvasScaler scale factors, and phones with notches/rounded
// corners never have a touch region placed under them.
[RequireComponent(typeof(RectTransform))]
public class MobileControlLayout : MonoBehaviour
{
    [Header("Control Regions")]
    [SerializeField] private RectTransform moveJoystickRect;
    [SerializeField] private RectTransform cameraLookAreaRect;
    [SerializeField] private RectTransform actionButtonsRect;

    private RectTransform canvasRect;
    private Vector2Int lastScreenSize;
    private Rect lastSafeArea;
    private bool hasAppliedOnce;

    private void Awake()
    {
        canvasRect = GetComponent<RectTransform>();

        if (moveJoystickRect == null)
            Debug.LogWarning($"{nameof(MobileControlLayout)} on '{name}' has no {nameof(moveJoystickRect)} assigned.", this);

        if (cameraLookAreaRect == null)
            Debug.LogWarning($"{nameof(MobileControlLayout)} on '{name}' has no {nameof(cameraLookAreaRect)} assigned.", this);
    }

    private void Update()
    {
        ApplyLayoutIfChanged();
    }

    private void ApplyLayoutIfChanged()
    {
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        Rect safeArea = Screen.safeArea;

        if (hasAppliedOnce && screenSize == lastScreenSize && safeArea == lastSafeArea)
            return;

        lastScreenSize = screenSize;
        lastSafeArea = safeArea;
        hasAppliedOnce = true;

        RecalculateRegions(screenSize, safeArea);
    }

    private void RecalculateRegions(Vector2Int screenSize, Rect safeAreaPixels)
    {
        if (canvasRect == null || screenSize.x <= 0 || screenSize.y <= 0)
            return;

        // CanvasScaler ("Scale With Screen Size") resizes the Canvas's own local rect relative to
        // the real screen size, so Canvas-local units are not 1:1 with Screen pixels except when the
        // current resolution happens to match the reference resolution exactly. Convert explicitly.
        Rect canvasLocalRect = canvasRect.rect;
        float unitsPerPixelX = canvasLocalRect.width / screenSize.x;
        float unitsPerPixelY = canvasLocalRect.height / screenSize.y;

        float safeMinX = canvasLocalRect.xMin + safeAreaPixels.xMin * unitsPerPixelX;
        float safeMinY = canvasLocalRect.yMin + safeAreaPixels.yMin * unitsPerPixelY;
        float usableWidth = safeAreaPixels.width * unitsPerPixelX;
        float usableHeight = safeAreaPixels.height * unitsPerPixelY;
        float halfWidth = usableWidth * 0.5f;
        float rightWidth = usableWidth - halfWidth;

        ApplyRegion(moveJoystickRect, canvasLocalRect,
            centerX: safeMinX + halfWidth * 0.5f,
            centerY: safeMinY + usableHeight * 0.5f,
            width: halfWidth,
            height: usableHeight);

        float rightCenterX = safeMinX + halfWidth + rightWidth * 0.5f;
        float rightCenterY = safeMinY + usableHeight * 0.5f;

        ApplyRegion(cameraLookAreaRect, canvasLocalRect,
            centerX: rightCenterX,
            centerY: rightCenterY,
            width: rightWidth,
            height: usableHeight);

        // ActionButtons shares the exact same right-safe-area footprint as CameraLookArea (it's a
        // later sibling, so it renders on top and intercepts its own touches first) — this does not
        // touch MoveJoystick's region at all.
        ApplyRegion(actionButtonsRect, canvasLocalRect,
            centerX: rightCenterX,
            centerY: rightCenterY,
            width: rightWidth,
            height: usableHeight);
    }

    private static void ApplyRegion(RectTransform rect, Rect canvasLocalRect, float centerX, float centerY, float width, float height)
    {
        if (rect == null)
            return;

        // Non-stretched single-point anchor: FloatingJoystick's own touch-position math depends on
        // sizeDelta holding the real touch-area size, which only holds for a non-stretched RectTransform.
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(centerX, centerY) - canvasLocalRect.min;
    }
}
