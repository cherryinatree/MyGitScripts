using UnityEngine;

public class DeliveryPoint : MonoBehaviour
{
    [Tooltip("Optional: Spawn will add slight offsets to avoid overlaps.")]
    public Vector3 perItemOffset = new Vector3(0.5f, 0f, 0.5f);

    public Vector3 GetSpawnPos(int index)
        => transform.position + perItemOffset * index;

    public Quaternion GetSpawnRot() => transform.rotation;
}
