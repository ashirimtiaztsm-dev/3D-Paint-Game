using System;
using UnityEngine;

// Marks a collider as a valid spray target. Flags whether it can currently receive paint, forwards
// each accepted spray hit to any listener (PaintableSurface stamps these into its RenderTexture),
// and exposes a stroke-interrupt signal so a listener can reset its "continuous stroke" state
// whenever the gun stops hitting this surface (miss, no paint, or firing stops entirely).
public class PaintSurfaceMarker : MonoBehaviour
{
    private const float GizmoLifetime = 1.5f;

    [SerializeField] private bool canReceivePaint = true;
    [SerializeField] private string targetId;

    private PaintSprayHit lastHit;
    private bool hasLastHit;
    private float lastHitTime;

    public bool CanReceivePaint => canReceivePaint;
    public string TargetId => targetId;

    public event Action<PaintSprayHit> SprayHitReceived;
    public event Action StrokeInterrupted;

    // Renamed parameter to avoid shadowing Behaviour.enabled-style naming conventions used elsewhere.
    public void SetCanReceivePaint(bool canReceive)
    {
        canReceivePaint = canReceive;
    }

    public void RegisterSprayHit(in PaintSprayHit hit)
    {
        if (!canReceivePaint)
            return;

        lastHit = hit;
        hasLastHit = true;
        lastHitTime = Time.time;
        SprayHitReceived?.Invoke(hit);
    }

    public void NotifyStrokeInterrupted()
    {
        StrokeInterrupted?.Invoke();
    }

    private void OnDrawGizmos()
    {
        if (!hasLastHit || Time.time - lastHitTime > GizmoLifetime)
            return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(lastHit.Point, 0.05f);
        Gizmos.DrawLine(lastHit.Point, lastHit.Point + lastHit.Normal * 0.3f);
    }
}
