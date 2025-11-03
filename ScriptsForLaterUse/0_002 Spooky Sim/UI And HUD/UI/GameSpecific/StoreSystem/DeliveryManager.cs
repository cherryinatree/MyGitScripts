using UnityEngine;

public class DeliveryManager : MonoBehaviour
{
    [SerializeField] private DeliveryPoint defaultPoint;
    [Tooltip("Optional: parent to keep hierarchy neat.")]
    [SerializeField] private Transform deliveryParent;

    public void DeliverProduct(GameObject prefab, int count = 1)
    {
        if (prefab == null || count <= 0 || defaultPoint == null) return;
        for (int i = 0; i < count; i++)
        {
            var pos = defaultPoint.GetSpawnPos(i);
            var rot = defaultPoint.GetSpawnRot();
            var go = GameObject.Instantiate(prefab, pos, rot, deliveryParent);
            // If you have a save/ID system for placed objects, initialize here.
        }

        // Optional: Rebuild NavMesh after large drops
        // var surf = FindObjectOfType<NavMeshSurface>(); if (surf) surf.BuildNavMesh();
    }
}
