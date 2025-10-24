using System.Collections.Generic;
using UnityEngine;

public class ChangeStack : MonoBehaviour
{
    [Header("Anchor & Prefab (optional)")]
    [Tooltip("If null, this object's transform is used as the stacking anchor/basis.")]
    public Transform anchor;
    [Tooltip("Optional default prefab used by PushClone(). You can also pass a prefab to PushClone().")]
    public GameObject defaultPrefab;

    [Header("Layout")]
    [Min(1)] public int perColumn = 50;          // how many items tall before starting new column
    [Min(1)] public int columnsPerRow = 5;       // how many columns along X before wrapping to next row (Z)
    [Tooltip("Thickness between stacked items (meters).")]
    public float itemHeight = 0.005f;
    [Tooltip("Spacing between columns (X) and rows (Z).")]
    public Vector2 columnGridSpacing = new Vector2(0.05f, 0.05f);

    [Header("Look")]
    [Tooltip("Small random local X/Z jitter so piles look natural.")]
    public Vector2 jitterXZ = new Vector2(0.0015f, 0.0015f);
    [Tooltip("Random yaw (deg) around local up. Bills look nice with ~1-3°; set to 0 for coins.")]
    public float yawJitterDegrees = 2f;

    [Header("Behavior")]
    [Tooltip("Make items kinematic while stacked (recommended for cosmetic piles).")]
    public bool makeKinematicOnStack = true;

    // ---------- runtime ----------
    private readonly List<Transform> _items = new();

    // per-item state to restore on pop/remove
    private class StackState : MonoBehaviour
    {
        public Transform originalParent;
        public bool hadRb;
        public bool rbWasKinematic;
        public bool rbUseGravity;
    }

    private void Awake()
    {
        if (!anchor) anchor = transform;
    }

    // ----- Public API -----

    /// <summary>Push an existing GameObject into the stack (reparents & lays out).</summary>
    public void Push(GameObject item)
    {
        if (!item) return;

        // Prepare state for restoration on pop/remove
        var state = item.GetComponent<StackState>();
        if (!state) state = item.AddComponent<StackState>();
        state.originalParent = item.transform.parent;

        var rb = item.GetComponent<Rigidbody>();
        state.hadRb = rb != null;
        if (rb)
        {
            state.rbWasKinematic = rb.isKinematic;
            state.rbUseGravity = rb.useGravity;
            if (makeKinematicOnStack)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        // Parent under anchor and place
        var t = item.transform;
        t.SetParent(anchor, worldPositionStays: true);
        _items.Add(t);
        PlaceAtIndex(_items.Count - 1, t);
    }

    /// <summary>Instantiate a prefab (or defaultPrefab) and push it. Returns the instance.</summary>
    public GameObject PushClone(GameObject prefab = null)
    {
        var p = prefab ? prefab : defaultPrefab;
        if (!p)
        {
            Debug.LogError($"{name}: PushClone called without a prefab.");
            return null;
        }
        var inst = Instantiate(p, anchor.position, anchor.rotation);
        Push(inst);
        return inst;
    }

    /// <summary>Pop (remove) the top item from the stack. Returns the GameObject (now unstacked) or null if empty.</summary>
    public GameObject Pop()
    {
        if (_items.Count == 0) return null;
        int last = _items.Count - 1;
        var t = _items[last];
        _items.RemoveAt(last);
        RestoreAndUnparent(t);
        return t ? t.gameObject : null;
    }

    /// <summary>Remove a specific item from the stack (wherever it is). Reflows remaining items.</summary>
    public bool Remove(GameObject item)
    {
        if (!item) return false;
        int idx = _items.IndexOf(item.transform);
        if (idx < 0) return false;

        _items.RemoveAt(idx);
        RestoreAndUnparent(item.transform);

        // Reflow positions for items after the removed index
        for (int i = idx; i < _items.Count; i++)
            PlaceAtIndex(i, _items[i]);

        return true;
    }

    /// <summary>Clear the stack (pops everything). If you want to delete them instead, destroy after popping.</summary>
    public void Clear()
    {
        for (int i = _items.Count - 1; i >= 0; i--)
            RestoreAndUnparent(_items[i]);
        _items.Clear();
    }

    public int Count => _items.Count;

    // ----- Internals -----

    private void PlaceAtIndex(int index, Transform t)
    {
        // column/row/level math
        int columnIndex = index / perColumn;   // 0..∞
        int level = index % perColumn;   // 0..perColumn-1
        int gridX = columnIndex % columnsPerRow;
        int gridZ = columnIndex / columnsPerRow;

        // base position/orientation in anchor's local axes
        Vector3 pos = anchor.position
                      + anchor.right * (gridX * columnGridSpacing.x)
                      + anchor.forward * (gridZ * columnGridSpacing.y)
                      + anchor.up * (level * itemHeight);

        // light jitter for natural look
        if (jitterXZ.sqrMagnitude > 0f)
        {
            pos += anchor.right * Random.Range(-jitterXZ.x, jitterXZ.x);
            pos += anchor.forward * Random.Range(-jitterXZ.y, jitterXZ.y);
        }

        Quaternion rot = anchor.rotation;
        if (yawJitterDegrees > 0f)
        {
            float yaw = Random.Range(-yawJitterDegrees, yawJitterDegrees);
            rot = Quaternion.AngleAxis(yaw, anchor.up) * rot;
        }

        t.SetPositionAndRotation(pos, rot);
    }

    private void RestoreAndUnparent(Transform t)
    {
        if (!t) return;

        var state = t.GetComponent<StackState>();
        if (state)
        {
            t.SetParent(state.originalParent, worldPositionStays: true);

            var rb = t.GetComponent<Rigidbody>();
            if (rb && makeKinematicOnStack)
            {
                rb.isKinematic = state.rbWasKinematic;
                rb.useGravity = state.rbUseGravity;
            }
            Destroy(state);
        }
        else
        {
            // No state? Just detach.
            t.SetParent(null, worldPositionStays: true);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (perColumn < 1) perColumn = 1;
        if (columnsPerRow < 1) columnsPerRow = 1;
        if (!anchor) anchor = transform;
    }

    private void OnDrawGizmosSelected()
    {
        var a = anchor ? anchor : transform;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        // Draw a small 3x3 grid preview at ground level
        for (int gx = 0; gx < 3; gx++)
            for (int gz = 0; gz < 3; gz++)
            {
                Vector3 p = a.position + a.right * (gx * columnGridSpacing.x) + a.forward * (gz * columnGridSpacing.y);
                Gizmos.DrawWireCube(p + a.up * (itemHeight * 0.5f), new Vector3(0.02f, itemHeight, 0.02f));
            }
    }
#endif
}
