using System.Collections.Generic;
using UnityEngine;

public class RoomCollectibleSpawner : MonoBehaviour
{
    [Header("Room Volume")]
    [SerializeField] private BoxCollider roomBounds; // should be trigger
    [SerializeField] private bool spawnOnStart = true;

    [Header("How Many")]
    [SerializeField] private bool useRange = false;
    [SerializeField] private int exactCount = 5;
    [SerializeField] private Vector2Int countRange = new Vector2Int(3, 8);

    [Header("Collectible Types")]
    [SerializeField] private List<CollectibleDefinition> collectibles = new();

    [Header("Surface Finding")]
    [SerializeField] private LayerMask surfaceMask;
    [SerializeField] private float maxRayDistance = 6f;
    [SerializeField] private int raySamplesPerItem = 12;

    [Header("Surface Classification")]
    [Range(0f, 1f)]
    [SerializeField] private float topDotThreshold = 0.6f;     // dot(up, normal) >= this => Top
    [Range(-1f, 0f)]
    [SerializeField] private float bottomDotThreshold = -0.6f; // dot(up, normal) <= this => Bottom

    [Header("Overlap / Blocking")]
    [SerializeField] private LayerMask blockingMask; // include collectibles layer here
    [SerializeField] private int maxPlacementAttemptsPerItem = 30;

    private readonly List<GameObject> spawned = new();

    private void Awake()
    {
        if (!roomBounds) roomBounds = GetComponent<BoxCollider>();
    }

    private void Start()
    {
        if (spawnOnStart)
            SpawnRoomCollectibles();
    }

    public void ClearSpawned()
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
            if (spawned[i]) Destroy(spawned[i]);
        spawned.Clear();
    }

    public void SpawnRoomCollectibles()
    {
        if (!roomBounds)
        {
            Debug.LogError($"{name}: No roomBounds assigned.");
            return;
        }

        int targetCount = useRange
            ? Random.Range(countRange.x, countRange.y + 1)
            : exactCount;

        for (int i = 0; i < targetCount; i++)
        {
            bool placed = TryPlaceOne(out GameObject go);
            if (placed && go) spawned.Add(go);
        }
    }

    private bool TryPlaceOne(out GameObject spawnedObj)
    {
        spawnedObj = null;
        if (collectibles == null || collectibles.Count == 0) return false;

        for (int attempt = 0; attempt < maxPlacementAttemptsPerItem; attempt++)
        {
            // 1) pick a random point in the room volume
            Vector3 samplePoint = RandomPointInBounds(roomBounds.bounds);

            // 2) shoot a handful of rays in random directions to find a surface
            for (int r = 0; r < raySamplesPerItem; r++)
            {
                Vector3 dir = Random.onUnitSphere;
                if (!Physics.Raycast(samplePoint, dir, out RaycastHit hit, maxRayDistance, surfaceMask,
                        QueryTriggerInteraction.Ignore))
                    continue;

                SurfaceCategory surfaceCat = ClassifySurface(hit);

                // 3) pick a collectible that is allowed on this surface
                CollectibleDefinition def = PickWeightedCollectible(surfaceCat);
                if (!def || !def.prefab) continue;

                // 4) compute pose
                if (!TryComputePose(def, hit, out Vector3 pos, out Quaternion rot, out Vector3 halfExtents))
                    continue;

                // 5) overlap check
                if (IsOverlapping(pos, rot, halfExtents, hit.collider))
                    continue;

                // 6) spawn!
                spawnedObj = Instantiate(def.prefab, pos, rot, transform);
                return true;
            }
        }

        return false;
    }

    private Vector3 RandomPointInBounds(Bounds b)
    {
        return new Vector3(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.min.y, b.max.y),
            Random.Range(b.min.z, b.max.z)
        );
    }

    private SurfaceCategory ClassifySurface(RaycastHit hit)
    {
        SurfaceMarker marker = hit.collider.GetComponentInParent<SurfaceMarker>();
        if (marker && marker.surfaceCategoryOverride != SurfaceCategory.None)
            return marker.surfaceCategoryOverride;

        float dotUp = Vector3.Dot(Vector3.up, hit.normal);

        if (dotUp >= topDotThreshold) return SurfaceCategory.Top;
        if (dotUp <= bottomDotThreshold) return SurfaceCategory.Bottom;
        return SurfaceCategory.Side;
    }

    private CollectibleDefinition PickWeightedCollectible(SurfaceCategory surfaceCat)
    {
        // filter by allowed surfaces first
        List<CollectibleDefinition> valid = new();
        int totalWeight = 0;

        foreach (var c in collectibles)
        {
            if (!c || !c.prefab) continue;
            if ((c.allowedSurfaces & surfaceCat) == 0) continue;

            valid.Add(c);
            totalWeight += Mathf.Max(1, c.weight);
        }

        if (valid.Count == 0) return null;

        int roll = Random.Range(0, totalWeight);
        int accum = 0;

        foreach (var v in valid)
        {
            accum += Mathf.Max(1, v.weight);
            if (roll < accum) return v;
        }

        return valid[valid.Count - 1];
    }

    private bool TryComputePose(
        CollectibleDefinition def,
        RaycastHit hit,
        out Vector3 position,
        out Quaternion rotation,
        out Vector3 clearanceHalfExtents)
    {
        position = hit.point;
        rotation = Quaternion.identity;
        clearanceHalfExtents = def.clearanceHalfExtents;

        // Find anchor (optional but recommended)
        CollectibleAnchor anchor = def.prefab.GetComponentInChildren<CollectibleAnchor>();

        // Base rotation: align bottom -> opposite of surface normal
        if (anchor)
        {
            Vector3 bottomWorld = anchor.BottomDirWorld(def.prefab.transform);
            rotation = Quaternion.FromToRotation(bottomWorld, -hit.normal);
            // Keep prefab's original orientation as baseline
            rotation *= def.prefab.transform.rotation;

            if (anchor.overridesClearanceExtents)
                clearanceHalfExtents = anchor.clearanceHalfExtents;
        }
        else
        {
            // default assumes prefab bottom is local down
            rotation = Quaternion.FromToRotation(Vector3.down, -hit.normal) * def.prefab.transform.rotation;
        }

        // random yaw around the normal if asked
        if (def.randomYaw)
            rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), hit.normal) * rotation;

        // Offset so it sits flush
        Vector3 offset = hit.normal * def.surfaceOffset;

        if (anchor)
        {
            // Move pivot so bottom contact point lands on surface
            Vector3 pivotOffsetWorld = rotation * anchor.pivotToBottomOffset;
            offset -= pivotOffsetWorld;
        }

        position += offset;

        return true;
    }

    private bool IsOverlapping(Vector3 pos, Quaternion rot, Vector3 halfExt, Collider surfaceCollider)
    {
        Collider[] hits = Physics.OverlapBox(
            pos,
            halfExt,
            rot,
            blockingMask,
            QueryTriggerInteraction.Ignore
        );

        foreach (var h in hits)
        {
            if (!h) continue;
            if (h == surfaceCollider) continue; // allow touching the surface we snapped to
            if (h.transform.IsChildOf(transform)) continue; // ignore other spawned items parented here? remove if undesired
            return true;
        }

        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (roomBounds)
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = roomBounds.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(roomBounds.center, roomBounds.size);
        }
    }
#endif
}
