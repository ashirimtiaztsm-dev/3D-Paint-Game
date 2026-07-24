using System;
using UnityEngine;

// Polls a fixed interval for PlayerInteractionZone colliders on the Interactable layer around
// InteractionOrigin using a non-allocating overlap query — no OnTrigger callbacks, no Rigidbody
// dependency on the Player. Selects the nearest available zone, using Priority only to break ties
// between comparably-close zones.
public class PlayerInteractionDetector : MonoBehaviour
{
    private const int MaxResults = 16;
    private const float TieDistanceEpsilon = 0.05f;

    [Header("References")]
    [SerializeField] private Transform interactionOrigin;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 1.75f;
    [SerializeField] private float scanInterval = 0.08f;
    [SerializeField] private LayerMask interactionMask;

    private readonly Collider[] resultsBuffer = new Collider[MaxResults];
    private float scanTimer;
    private PlayerInteractionZone currentZone;

    public PlayerInteractionZone CurrentZone => currentZone;
    public event Action<PlayerInteractionZone> SelectedZoneChanged;

    private void Awake()
    {
        if (interactionOrigin == null)
            Debug.LogWarning($"{nameof(PlayerInteractionDetector)} on '{name}' has no {nameof(interactionOrigin)} assigned.", this);
    }

    private void Update()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer > 0f)
            return;

        scanTimer = scanInterval;
        Scan();
    }

    private void Scan()
    {
        if (interactionOrigin == null)
        {
            SetCurrentZone(null);
            return;
        }

        int count = Physics.OverlapSphereNonAlloc(
            interactionOrigin.position,
            detectionRadius,
            resultsBuffer,
            interactionMask,
            QueryTriggerInteraction.Collide);

        PlayerInteractionZone best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider hitCollider = resultsBuffer[i];
            if (hitCollider == null)
                continue;

            PlayerInteractionZone zone = hitCollider.GetComponentInParent<PlayerInteractionZone>();
            if (zone == null || !zone.IsAvailable)
                continue;

            float distance = Vector3.Distance(zone.InteractionPoint, interactionOrigin.position);

            if (best == null || distance < bestDistance - TieDistanceEpsilon)
            {
                best = zone;
                bestDistance = distance;
            }
            else if (Mathf.Abs(distance - bestDistance) <= TieDistanceEpsilon && zone.Priority > best.Priority)
            {
                best = zone;
                bestDistance = distance;
            }
        }

        SetCurrentZone(best);
    }

    private void SetCurrentZone(PlayerInteractionZone zone)
    {
        if (currentZone == zone)
            return;

        currentZone = zone;
        SelectedZoneChanged?.Invoke(currentZone);
    }

    private void OnDrawGizmosSelected()
    {
        if (interactionOrigin == null)
            return;

        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(interactionOrigin.position, detectionRadius);
    }
}
