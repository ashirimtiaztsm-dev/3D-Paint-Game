using UnityEngine;

// Pure descriptor for an interactable area — what action it offers, where, and whether it's
// currently usable. Contains no Fill/Fire behaviour; PlayerInteractionDetector reads these fields
// to decide what the player is near, and later stages (Paint Tank / Paint Target) drive gameplay
// off the selected zone from the outside.
public class PlayerInteractionZone : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private InteractionActionType actionType = InteractionActionType.None;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private int priority;
    [SerializeField] private bool isAvailable = true;
    [SerializeField] private string promptText;

    [Header("Editor Visualization")]
    [SerializeField] private Color gizmoColor = Color.yellow;
    [SerializeField] private float gizmoRadius = 0.3f;

    public InteractionActionType ActionType => actionType;
    public Vector3 InteractionPoint => interactionPoint != null ? interactionPoint.position : transform.position;
    public int Priority => priority;
    public bool IsAvailable => isAvailable;
    public string PromptText => promptText;

    public void SetAvailable(bool available)
    {
        isAvailable = available;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(InteractionPoint, gizmoRadius);
    }
}
