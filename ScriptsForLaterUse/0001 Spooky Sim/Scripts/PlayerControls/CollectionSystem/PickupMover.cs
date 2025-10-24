using Cherry.Inventory;
using System;
using UnityEngine;

[AddComponentMenu("Items/Pickup Mover")]
public class PickupMover : MonoBehaviour
{
    [Header("Travel")]
    [Min(0.01f)] public float travelDuration = 0.25f;
    [Tooltip("If true, the pickup will home to the (moving) target each frame.")]
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
    private float _t;
    private Action<bool> _onArrive;

    public void Initialize(ItemDefinition item, int amount, Transform target, Action<bool> onArrive)
    {
        _item = item;
        _amount = amount;
        _target = target;
        _startPos = transform.position;
        _t = 0f;
        _onArrive = onArrive;
    }

    private void Update()
    {
        if (_target == null) { Finish(false); return; }

        _t += Time.deltaTime / Mathf.Max(0.01f, travelDuration);
        float u = Mathf.Clamp01(_t);

        Vector3 endPos = _target.position;
        Vector3 straight = Vector3.Lerp(_startPos, endPos, u);

        if (arcHeight > 0f)
        {
            // simple parabola: add vertical arc based on u*(1-u)
            float arc = 4f * (u * (1f - u)) * arcHeight;
            straight.y += arc;
        }

        transform.position = straight;

        // Optional “homing” keeps chasing a moving target; if false, we used a snapshot endPos above.
        if (homeToMovingTarget == false && u < 1f)
        {
            // (no-op; straight already uses snapshot)
        }

        if (u >= 1f)
        {
            // Try to add to inventory
            bool success = true;
            if (addToInventoryOnArrive)
            {
                var sink = FindInventorySink(_target);
                if (sink != null)
                {
                    success = sink.AddItem(_item, _amount);
                }
                else
                {
                    Debug.LogWarning($"{name}: No IInventorySink found near target '{_target.name}'.");
                    success = false;
                }
            }

            Finish(success);
        }
    }

    private void Finish(bool success)
    {
        try { _onArrive?.Invoke(success); }
        finally { Destroy(gameObject); }
    }

    private IInventorySink FindInventorySink(Transform t)
    {
        // Look on target, parents, then scene as last resort
        var sink = t.GetComponentInParent<IInventorySink>();
        if (sink != null) return sink;
        return FindObjectOfType<MonoBehaviour>(true) as IInventorySink; // ok if none
    }
}
