using UnityEngine;
using Cherry.Inventory; // <- for StorageContainer (and optionally IBeamHarvestable)

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

        Transform origin = ResolveBeamOrigin();

        // 1) STORAGE CONTAINER: harvest items stored inside it
        var container = hit.collider.GetComponentInParent<StorageContainer>();
        if (container != null)
        {
            container.TryHarvestFromBeam(hit.point, origin);
            return;
        }

        // 2) LEGACY: harvestable item sources
        var source = hit.collider.GetComponentInParent<HarvestableItemSource>();
        if (source == null) return;

        source.TryHarvestFromBeam(hit.point, origin);
    }

    private Transform ResolveBeamOrigin()
    {
        if (beamOrigin != null) return beamOrigin;

        var cam = Camera.main;
        if (cam != null) return cam.transform;

        return transform; // never return null
    }
}
