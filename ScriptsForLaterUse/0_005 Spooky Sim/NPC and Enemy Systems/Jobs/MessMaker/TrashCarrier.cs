using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TrashCarrier : MonoBehaviour
{
    [Header("Carry")]
    public Transform CarrySocket;
    public int Capacity = 10;

    [Header("Stacking")]
    public float StackSpacing = 0.18f;
    public Vector3 LocalStackAxis = Vector3.up;

    private readonly List<GameObject> _items = new();
    public int Count => _items.Count;
    public bool IsFull => Count >= Capacity;

    public bool CanTake(int amount = 1) => (Count + amount) <= Capacity;

    public bool Pickup(MessTrash trash)
    {
        if (trash == null) return false;
        if (!CanTake(1)) return false;

        GameObject go = trash.gameObject;

        // Remove it from the job registry so nobody else tries to take it
        trash.MarkTaken();

        // Disable physics so it doesn't fight the bot
        if (go.transform.GetChild(0).TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        foreach (var col in go.GetComponentsInChildren<Collider>())
            col.enabled = false;

        // Parent it to carry socket
        if (CarrySocket == null) CarrySocket = transform;
        go.transform.SetParent(CarrySocket, worldPositionStays: false);

        // Stack nicely
        Vector3 axis = LocalStackAxis.sqrMagnitude < 0.001f ? Vector3.up : LocalStackAxis.normalized;
        go.transform.localPosition = axis * (StackSpacing * _items.Count);
        go.transform.localRotation = Quaternion.identity;

        _items.Add(go);
        return true;
    }

    /// <summary>Drops all carried trash into a container (airlock chamber).</summary>
    public List<GameObject> DropAllInto(Transform container, Vector3 dropCenterWorld, float scatterRadius = 0.25f)
    {
        var dropped = new List<GameObject>(_items.Count);
        Debug.Log(33333);
        for (int i = 0; i < _items.Count; i++)
        {
            var go = _items[i];
            if (go == null) continue;


            if (go.transform.GetChild(0).TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = false;
            }

            go.GetComponent<MessTrash>().enabled = false;



            go.transform.SetParent(container, worldPositionStays: true);

            // Place with small random scatter
            Vector2 s = Random.insideUnitCircle * scatterRadius;
            go.transform.position = dropCenterWorld + new Vector3(s.x, 0f, s.y);

            // Re-enable colliders so it looks like it's really in the chamber
            foreach (var col in go.GetComponentsInChildren<Collider>())
                col.enabled = true;

            // Keep kinematic until vent (airlock will decide what to do)
            dropped.Add(go);
        }

        _items.Clear();
        return dropped;
    }
}
