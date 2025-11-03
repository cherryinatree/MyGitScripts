using UnityEngine;

[AddComponentMenu("Gameplay/Beam Hit Router")]
public class BeamHitRouter : MonoBehaviour
{
    [Tooltip("Where the beam starts. Use the SAME origin as your raycaster (camera or muzzle).")]
    [SerializeField] private Transform beamOrigin;

    [Tooltip("Optional: only harvest objects on these layers.")]
    [SerializeField] private LayerMask harvestableMask = ~0;

    public void HandleHit(RaycastHit hit)
    {
        if (((1 << hit.collider.gameObject.layer) & harvestableMask) == 0) return;

        var source = hit.collider.GetComponentInParent<HarvestableItemSource>();
        if (source == null) return;

        // Tell the source to spawn a pickup that flies back to beam origin
        if (beamOrigin == null)
        {
            var cam = Camera.main;
            beamOrigin = cam ? cam.transform : null;
        }

        source.TryHarvestFromBeam(hit.point, beamOrigin);
    }
}
