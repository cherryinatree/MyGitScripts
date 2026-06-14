using UnityEngine;
using Cherry.Inventory;
using System.Collections.Generic;

[AddComponentMenu("Gameplay/Beam Hit Router")]
public class BeamHitRouter : MonoBehaviour
{
    [SerializeField] private Transform beamOrigin;
    [SerializeField] private LayerMask harvestableMask = ~0;

    public void HandleHit(RaycastHit hit)
        => HandleHit(hit, null);

    public void HandleHit(RaycastHit hit, IReadOnlyList<Vector3> returnPath)
    {
        if (((1 << hit.collider.gameObject.layer) & harvestableMask) == 0) return;

        Transform origin = ResolveBeamOrigin();

        var container = hit.collider.GetComponentInParent<StorageContainer>();
        if (container != null)
        {
            // IMPORTANT: If you harvest containers too, you need a path-aware overload there as well.
            container.TryHarvestFromBeam(hit.point, origin); // <- will still go straight unless you add an overload
            return;
        }

        var source = hit.collider.GetComponentInParent<HarvestableItemSource>();
        if (source == null) return;

        source.TryHarvestFromBeam(hit.point, origin, returnPath); // <- THIS is what triggers path-following
    }

    private Transform ResolveBeamOrigin()
    {
        if (beamOrigin != null) return beamOrigin;
        var cam = Camera.main;
        if (cam != null) return cam.transform;
        return transform;
    }
}