using UnityEngine;

/// <summary>
/// Defines an area where visual topping pieces can be scattered.
/// Put this on an invisible child object above the top ice cream scoop or above the pizza topping area.
/// </summary>
public class FoodToppingScatterZone : MonoBehaviour
{
    public enum ScatterSurfaceMode
    {
        FlatCircle,
        FlatRectangle,
        UpperHemisphere,
        ColliderRaycast
    }

    [Header("Shape")]
    public ScatterSurfaceMode surfaceMode = ScatterSurfaceMode.FlatCircle;

    [Tooltip("Used by FlatCircle and UpperHemisphere.")]
    public float radius = 0.12f;

    [Tooltip("Used by FlatRectangle. X = local width, Y = local depth.")]
    public Vector2 rectangleSize = new Vector2(0.25f, 0.25f);

    [Header("Collider Raycast Surface")]
    [Tooltip("Optional. If using ColliderRaycast, this should be the scoop/pizza collider to raycast onto.")]
    public Collider surfaceCollider;

    [Tooltip("How high above the random X/Z point to start raycasting.")]
    public float rayStartHeight = 0.35f;

    [Tooltip("How far below the random X/Z point the ray can search.")]
    public float rayDistance = 0.75f;

    [Header("Placement")]
    [Tooltip("Small offset away from the surface so toppings do not z-fight or sink into the food.")]
    public float surfaceOffset = 0.01f;

    [Tooltip("If true, topping pieces are rotated so their local up points away from the surface.")]
    public bool alignToSurfaceNormal = true;

    [Tooltip("Random yaw spin around the surface normal.")]
    public bool randomYaw = true;

    [Header("Debug")]
    public bool drawGizmos = true;

    /// <summary>
    /// Gets a random world-space point and rotation on this topping zone.
    /// </summary>
    public bool TryGetRandomPlacement(out Vector3 position, out Quaternion rotation)
    {
        Vector3 normal;
        position = GetRandomPoint(out normal);

        if (alignToSurfaceNormal)
        {
            rotation = Quaternion.FromToRotation(Vector3.up, normal);
        }
        else
        {
            rotation = transform.rotation;
        }

        if (randomYaw)
            rotation = rotation * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        return true;
    }

    private Vector3 GetRandomPoint(out Vector3 normal)
    {
        switch (surfaceMode)
        {
            case ScatterSurfaceMode.FlatRectangle:
                return GetFlatRectanglePoint(out normal);

            case ScatterSurfaceMode.UpperHemisphere:
                return GetUpperHemispherePoint(out normal);

            case ScatterSurfaceMode.ColliderRaycast:
                return GetColliderRaycastPoint(out normal);

            case ScatterSurfaceMode.FlatCircle:
            default:
                return GetFlatCirclePoint(out normal);
        }
    }

    private Vector3 GetFlatCirclePoint(out Vector3 normal)
    {
        Vector2 p = Random.insideUnitCircle * radius;
        normal = transform.up;
        return transform.TransformPoint(new Vector3(p.x, surfaceOffset, p.y));
    }

    private Vector3 GetFlatRectanglePoint(out Vector3 normal)
    {
        float x = Random.Range(-rectangleSize.x * 0.5f, rectangleSize.x * 0.5f);
        float z = Random.Range(-rectangleSize.y * 0.5f, rectangleSize.y * 0.5f);
        normal = transform.up;
        return transform.TransformPoint(new Vector3(x, surfaceOffset, z));
    }

    private Vector3 GetUpperHemispherePoint(out Vector3 normal)
    {
        // Random point on the top half of a sphere, useful for rounded ice cream scoops.
        Vector2 p = Random.insideUnitCircle * radius;
        float y = Mathf.Sqrt(Mathf.Max(0f, radius * radius - p.x * p.x - p.y * p.y));

        Vector3 localPoint = new Vector3(p.x, y, p.y);
        Vector3 localNormal = radius > 0.0001f ? localPoint.normalized : Vector3.up;

        normal = transform.TransformDirection(localNormal).normalized;
        return transform.TransformPoint(localPoint + localNormal * surfaceOffset);
    }

    private Vector3 GetColliderRaycastPoint(out Vector3 normal)
    {
        if (surfaceCollider == null)
            return GetFlatCirclePoint(out normal);

        Vector2 p = Random.insideUnitCircle * radius;
        Vector3 localXZ = new Vector3(p.x, 0f, p.y);
        Vector3 worldBase = transform.TransformPoint(localXZ);
        Vector3 rayOrigin = worldBase + transform.up * rayStartHeight;
        Ray ray = new Ray(rayOrigin, -transform.up);

        if (surfaceCollider.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            normal = hit.normal.normalized;
            return hit.point + normal * surfaceOffset;
        }

        return GetFlatCirclePoint(out normal);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Gizmos.matrix = transform.localToWorldMatrix;

        switch (surfaceMode)
        {
            case ScatterSurfaceMode.FlatRectangle:
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(rectangleSize.x, 0.01f, rectangleSize.y));
                break;

            case ScatterSurfaceMode.UpperHemisphere:
            case ScatterSurfaceMode.ColliderRaycast:
            case ScatterSurfaceMode.FlatCircle:
            default:
                DrawCircleGizmo(radius);
                break;
        }
    }

    private void DrawCircleGizmo(float r)
    {
        const int segments = 32;
        Vector3 previous = new Vector3(r, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments * Mathf.PI * 2f;
            Vector3 next = new Vector3(Mathf.Cos(t) * r, 0f, Mathf.Sin(t) * r);
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
