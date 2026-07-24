using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Tracks press-and-hold input state for a single contextual action button. Reports HoldStarted /
// HoldEnded and IsHeld only — never performs Fill/Fire gameplay itself.
public class HoldActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ICancelHandler
{
    private const int NoActivePointer = int.MinValue;

    private int activePointerId = NoActivePointer;
    private bool interactable = true;

    public bool IsHeld => activePointerId != NoActivePointer;

    public event Action HoldStarted;
    public event Action HoldEnded;

    private void OnDisable()
    {
        ReleaseHold();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            ReleaseHold();
    }

    // Renamed parameter to avoid shadowing Behaviour.enabled.
    public void SetInteractable(bool isInteractable)
    {
        interactable = isInteractable;

        if (!interactable)
            ReleaseHold();
    }

    // Allows an external owner (e.g. ContextualActionUI) to force a release when swapping which
    // action is active, so no stale held state survives an interaction change.
    public void ForceRelease()
    {
        ReleaseHold();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!interactable)
            return;

        if (activePointerId != NoActivePointer)
            return;

        activePointerId = eventData.pointerId;
        HoldStarted?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        ReleaseHold();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;

        ReleaseHold();
    }

    public void OnCancel(BaseEventData eventData)
    {
        ReleaseHold();
    }

    private void ReleaseHold()
    {
        if (activePointerId == NoActivePointer)
            return;

        activePointerId = NoActivePointer;
        HoldEnded?.Invoke();
    }
}
