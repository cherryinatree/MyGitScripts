using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
public class JanitorSenseBubble : MonoBehaviour
{
    private SphereCollider _col;
    private Rigidbody _rb;

    // Use HashSet so we don't double-add
    private readonly HashSet<MessItem> _nearby = new();

    public float Radius
    {
        get => _col != null ? _col.radius : 0f;
        set { if (_col != null) _col.radius = Mathf.Max(0.1f, value); }
    }

    private void Awake()
    {
        _col = GetComponent<SphereCollider>();
        _col.isTrigger = true;

        // Make trigger callbacks reliable: one side must have a Rigidbody
        _rb = GetComponent<Rigidbody>();
        if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity = false;
    }

    private void OnDisable()
    {
        _nearby.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        var m = other.GetComponentInParent<MessItem>();
        if (m == null) return;
        if (m.IsResolved) return;

        _nearby.Add(m);
    }

    private void OnTriggerExit(Collider other)
    {
        var m = other.GetComponentInParent<MessItem>();
        if (m == null) return;
        _nearby.Remove(m);
    }

    public void CleanupNulls()
    {
        _nearby.RemoveWhere(m => m == null || m.IsResolved);
    }

    public bool TryGetBest(Vector3 from, TrashCarrier carrier, out MessItem best)
    {
        CleanupNulls();

        best = null;
        float bestD = float.PositiveInfinity;

        foreach (var m in _nearby)
        {
            if (m == null || m.IsResolved) continue;
            if (m.IsClaimed) continue;

            // If we're full, ignore trash (we can't pick it up)
            if (carrier != null && carrier.IsFull && m is MessTrash) continue;

            float d = (m.JobPoint - from).sqrMagnitude;
            if (d < bestD)
            {
                bestD = d;
                best = m;
            }
        }

        return best != null;
    }

    public bool HasAnyCandidate(TrashCarrier carrier)
    {
        CleanupNulls();

        foreach (var m in _nearby)
        {
            if (m == null || m.IsResolved) continue;
            if (m.IsClaimed) continue;
            if (carrier != null && carrier.IsFull && m is MessTrash) continue;
            return true;
        }
        return false;
    }
}
