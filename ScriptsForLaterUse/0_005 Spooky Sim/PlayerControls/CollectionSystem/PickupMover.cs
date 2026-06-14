using Cherry.Inventory;
using System;
using UnityEngine;

[AddComponentMenu("Items/Pickup Mover")]
public class PickupMover : MonoBehaviour
{
    [Header("Travel")]
    [Tooltip("Units per second. Constant speed regardless of distance.")]
    [Min(0.01f)] public float travelSpeed = 8f;

    [Tooltip("Optional: do not allow arrival until this much time has passed. Does NOT change speed.")]
    [Min(0f)] public float minTravelTime = 0f;

    [Tooltip("How close we must be to consider 'arrived'. Keep this smaller than your proxy separation.")]
    [Min(0.001f)] public float arriveDistance = 0.05f;

    [Tooltip("If true, the pickup will chase the (moving) target each frame. Recommended for beam-path proxy targets.")]
    public bool homeToMovingTarget = true;

    [Tooltip("Optional arc height for a slight curve. 0 = straight line.")]
    [Min(0f)] public float arcHeight = 0f;

    [Header("Arrival")]
    [Tooltip("If true, we’ll search for an IInventorySink on the target (or its parents) to add the item.")]
    public bool addToInventoryOnArrive = true;

    private ItemDefinition _item;
    private int _amount;
    private Transform _target;
    private Vector3 _startPos;
    private Vector3 _snapshotEndPos;
    private float _elapsed;
    private float _initialDist;
    private Action<bool> _onArrive;

    public void Initialize(ItemDefinition item, int amount, Transform target, Action<bool> onArrive)
    {
        _item = item;
        _amount = amount;
        _target = target;
        _onArrive = onArrive;

        _startPos = transform.position;
        _snapshotEndPos = (_target != null) ? _target.position : _startPos;

        _elapsed = 0f;
        _initialDist = Vector3.Distance(_startPos, _snapshotEndPos);
        if (_initialDist < 0.001f) _initialDist = 0.001f;
    }

    private void Update()
    {
        if (_target == null) { Finish(false); return; }

        _elapsed += Time.deltaTime;

        Vector3 endPos = homeToMovingTarget ? _target.position : _snapshotEndPos;

        // Move at constant units/sec
        Vector3 next = Vector3.MoveTowards(transform.position, endPos, travelSpeed * Time.deltaTime);

        // Optional arc (purely visual)
        if (arcHeight > 0f)
        {
            float dist = Vector3.Distance(transform.position, endPos);
            float u = 1f - (dist / Mathf.Max(0.001f, _initialDist));
            u = Mathf.Clamp01(u);
            float arc = 4f * (u * (1f - u)) * arcHeight;
            next.y += arc;
        }

        transform.position = next;

        // Arrival check (with optional minimum time gate)
        float remaining = Vector3.Distance(transform.position, endPos);
        if (remaining <= arriveDistance && _elapsed >= minTravelTime)
        {
            Arrive();
        }
    }

    private void Arrive()
    {
        bool success = true;

        if (addToInventoryOnArrive)
        {
            var sink = FindInventorySink(_target);
            if (sink != null)
                success = sink.AddItem(_item, _amount);
            else
            {
                Debug.LogWarning($"{name}: No IInventorySink found near target '{_target.name}'.");
                success = false;
            }
        }

        Finish(success);
    }

    private void Finish(bool success)
    {
        try { _onArrive?.Invoke(success); }
        finally { Destroy(gameObject); }
    }

    private IInventorySink FindInventorySink(Transform t)
    {
        if (t == null) return null;

        // Prefer target/parents
        var monos = t.GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < monos.Length; i++)
            if (monos[i] is IInventorySink sink) return sink;

        // Last resort scene search (only runs on arrival)
        var all = FindObjectsOfType<MonoBehaviour>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i] is IInventorySink sink) return sink;

        return null;
    }
}