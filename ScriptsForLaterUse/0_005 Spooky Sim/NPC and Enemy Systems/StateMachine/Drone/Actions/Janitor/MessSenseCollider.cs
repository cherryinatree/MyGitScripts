using UnityEngine;

[DisallowMultipleComponent]
public class MessSenseCollider : MonoBehaviour
{
    public float Radius = 0.35f;
    public Vector3 LocalCenter = new Vector3(0f, 0.05f, 0f);

    private void Awake()
    {
        // If there's already any collider, you can skip this.
        // But decals often have none, so we add a tiny trigger.
        var existing = GetComponentInChildren<Collider>();
        if (existing != null) return;

        var sc = gameObject.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = Radius;
        sc.center = LocalCenter;
    }
}
