using UnityEngine;

// Performs the spray raycast and drives the two reusable particle systems. Never touches the
// reservoir and never reads the Fire button — PaintGunFireController decides *whether* to fire and
// how much paint to consume; this script only turns that decision into a raycast + visuals.
public class PaintSprayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform sprayOrigin;
    [SerializeField] private ParticleSystem sprayParticles;
    [SerializeField] private ParticleSystem impactParticles;

    [Header("Raycast")]
    [SerializeField] private float maxSprayDistance = 3f;
    [SerializeField] private LayerMask paintSurfaceMask;
    [SerializeField] private float impactNormalOffset = 0.02f;
    [SerializeField] private bool debugRay;

    private bool impactVisible;

    private void Awake()
    {
        if (sprayOrigin == null)
            Debug.LogWarning($"{nameof(PaintSprayer)} on '{name}' has no {nameof(sprayOrigin)} assigned.", this);

        StopSprayVisual();
    }

    // One Physics.Raycast per call. Only returns true when the ray hits a collider on the
    // PaintSurface layer whose PaintSurfaceMarker currently allows receiving paint.
    public bool TryGetValidHit(out RaycastHit hit, out PaintSurfaceMarker surface)
    {
        surface = null;

        if (sprayOrigin == null)
        {
            hit = default;
            return false;
        }

        bool didHit = Physics.Raycast(
            sprayOrigin.position,
            sprayOrigin.forward,
            out hit,
            maxSprayDistance,
            paintSurfaceMask,
            QueryTriggerInteraction.Ignore);

        if (debugRay)
            Debug.DrawRay(sprayOrigin.position, sprayOrigin.forward * maxSprayDistance, didHit ? Color.green : Color.red);

        if (!didHit)
            return false;

        surface = hit.collider.GetComponentInParent<PaintSurfaceMarker>();
        return surface != null && surface.CanReceivePaint;
    }

    // Starts the continuous spray stream for a firing session. Called once when firing begins.
    public void BeginSprayVisual(PaintColorDefinition paint)
    {
        ApplyParticleColor(sprayParticles, paint);

        if (sprayParticles != null && !sprayParticles.isPlaying)
            sprayParticles.Play();
    }

    // Ends the whole firing session: stops the spray stream and hides any impact visual.
    public void StopSprayVisual()
    {
        if (sprayParticles != null && sprayParticles.isPlaying)
            sprayParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        HideImpactVisual();
    }

    // Called once per frame while actively hitting a valid surface: repositions/re-colours the
    // impact effect and forwards a debug hit to the surface marker. Never touches the reservoir.
    public void EmitSprayHit(PaintColorDefinition paint, float consumedAmount, in RaycastHit hit, PaintSurfaceMarker surface)
    {
        ShowImpact(hit, paint);

        if (surface != null)
        {
            var sprayHit = new PaintSprayHit(paint, consumedAmount, hit.point, hit.normal, hit.textureCoord, hit.collider);
            surface.RegisterSprayHit(sprayHit);
        }
    }

    // Hides just the impact effect for a frame where the ray misses, without stopping the overall
    // spray stream (the session may still be actively firing, just not currently landing on anything).
    public void HideImpactVisual()
    {
        if (!impactVisible)
            return;

        if (impactParticles != null && impactParticles.isPlaying)
            impactParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        impactVisible = false;
    }

    private void ShowImpact(RaycastHit hit, PaintColorDefinition paint)
    {
        if (impactParticles == null)
            return;

        Vector3 position = hit.point + hit.normal * impactNormalOffset;
        impactParticles.transform.SetPositionAndRotation(position, Quaternion.LookRotation(hit.normal));

        ApplyParticleColor(impactParticles, paint);

        if (!impactParticles.isPlaying)
            impactParticles.Play();

        impactVisible = true;
    }

    private static void ApplyParticleColor(ParticleSystem particles, PaintColorDefinition paint)
    {
        if (particles == null || paint == null)
            return;

        ParticleSystem.MainModule main = particles.main;
        main.startColor = paint.DisplayColor;
    }
}
