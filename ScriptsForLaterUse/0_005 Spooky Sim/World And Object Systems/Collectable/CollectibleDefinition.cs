using UnityEngine;

[CreateAssetMenu(menuName = "Collectibles/Definition")]
public class CollectibleDefinition : ScriptableObject
{
    public GameObject prefab;

    [Header("Surface Rules")]
    public SurfaceCategory allowedSurfaces = SurfaceCategory.Top;

    [Tooltip("Extra distance to offset from hit surface to avoid z-fighting.")]
    public float surfaceOffset = 0.002f;

    [Tooltip("Random spin around the surface normal.")]
    public bool randomYaw = true;

    [Header("Overlap / Clearance")]
    [Tooltip("Half-extents for overlap checks (used if prefab doesn't override).")]
    public Vector3 clearanceHalfExtents = new Vector3(0.12f, 0.12f, 0.12f);

    [Header("Selection Weight")]
    [Min(1)] public int weight = 1;
}
