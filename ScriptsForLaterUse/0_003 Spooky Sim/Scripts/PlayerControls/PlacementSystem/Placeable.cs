using System;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class Placeable : MonoBehaviour
{
    public enum PlacementType { Floor, Wall }

    [Header("Identity & Save")]
    [SerializeField, Tooltip("Stable unique id; auto-filled if empty. Do NOT duplicate in scene.")]
    private string uniqueId;
    [SerializeField, Tooltip("Resources path or Addressables key to respawn this prefab on load (e.g. \"Store/Shelf_A\").")]
    private string prefabKey = "Store/Shelf_A";

    [Header("Placement")]
    public PlacementType placementType = PlacementType.Floor;
    [Tooltip("Optional custom pivot used during snapping. If null, transform is used.")]
    public Transform snapOrigin;
    [Tooltip("Small offset used when sticking to wall/floor to avoid z-fighting.")]
    public float surfaceOffset = 0.01f;

    [Header("NavMesh")]
    [Tooltip("Adds/uses NavMeshObstacle with carving so agents avoid this object when placed.")]
    public bool addNavMeshObstacle = true;
    [Tooltip("If true, also ask the runtime rebuilder to rebake NavMesh surfaces (debounced).")]
    public bool alsoRequestSurfaceRebuild = false;

    public string UniqueId => uniqueId;
    public string PrefabKey => prefabKey;

    public Bounds WorldBounds { get; private set; }

    public static event Action<Placeable> OnPlacedOrMoved;

    void Awake()
    {
        if (string.IsNullOrWhiteSpace(uniqueId))
            uniqueId = Guid.NewGuid().ToString("N");

        if (addNavMeshObstacle)
        {
            var obst = GetComponent<NavMeshObstacle>();
            if (!obst) obst = gameObject.AddComponent<NavMeshObstacle>();
            // Use a capsule or box: box fits shelves/fixtures better
            obst.carving = true;
            obst.carveOnlyStationary = true;
            // If there's a collider, approximate shape automatically
            var col = GetComponentInChildren<Collider>();
            if (col is BoxCollider b)
            {
                obst.shape = NavMeshObstacleShape.Box;
                obst.size = b.size;
                obst.center = b.center;
            }
            else if (col is CapsuleCollider c)
            {
                obst.shape = NavMeshObstacleShape.Capsule;
                obst.radius = c.radius;
                obst.height = c.height;
                obst.center = c.center;
            }
        }

        RecalcBounds();
    }

    public void RecalcBounds()
    {
        var cols = GetComponentsInChildren<Collider>();
        if (cols.Length == 0) { WorldBounds = new Bounds(transform.position, Vector3.one * 0.5f); return; }
        var b = new Bounds(cols[0].bounds.center, cols[0].bounds.size);
        for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
        WorldBounds = b;
    }

    public void NotifyPlacedOrMoved()
    {
        RecalcBounds();
        OnPlacedOrMoved?.Invoke(this);
        if (alsoRequestSurfaceRebuild)
            NavMeshRuntimeRebuilder.RequestRebuild();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (snapOrigin == null) snapOrigin = transform;
    }
#endif
}
