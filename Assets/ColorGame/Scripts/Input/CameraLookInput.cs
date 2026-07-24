using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CameraLookInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private const int NoActivePointer = int.MinValue;

    private int activePointerId = NoActivePointer;
    private Vector2 accumulatedDelta;
    private bool lookEnabled = true;

    // Renamed parameter to avoid shadowing Component.enabled.
    public void SetLookEnabled(bool isEnabled)
    {
        lookEnabled = isEnabled;

        if (!lookEnabled)
            ResetPointerState();
    }

    private void OnDisable()
    {
        ResetPointerState();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            ResetPointerState();
    }

    private void ResetPointerState()
    {
        activePointerId = NoActivePointer;
        accumulatedDelta = Vector2.zero;
    }

    // Called once per frame by the camera to consume screen-space drag delta (touch/left-click on this
    // panel, or right-mouse-drag anywhere) accumulated since the previous call.
    public Vector2 ConsumeLookDelta()
    {
        if (!lookEnabled)
        {
            accumulatedDelta = Vector2.zero;
            return Vector2.zero;
        }

        Vector2 delta = accumulatedDelta + ReadMouseRightDragDelta();
        accumulatedDelta = Vector2.zero;
        return delta;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!lookEnabled)
            return;

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // Only the first finger/click to land on this panel drives the camera until it's released;
        // a second touch landing here while the first is still down is ignored.
        if (activePointerId != NoActivePointer)
            return;

        activePointerId = eventData.pointerId;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!lookEnabled)
            return;

        if (eventData.pointerId != activePointerId)
            return;

        accumulatedDelta += eventData.delta;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        activePointerId = NoActivePointer;
    }

    private Vector2 ReadMouseRightDragDelta()
    {
        if (!lookEnabled)
            return Vector2.zero;

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.rightButton.isPressed)
            return Vector2.zero;

        return mouse.delta.ReadValue();
    }
}
