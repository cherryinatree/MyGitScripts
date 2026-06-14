using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Box that can be carried (has Carryable) and stores other Carryables.
/// Items are deactivated when stored and reactivated when popped.
/// </summary>
[RequireComponent(typeof(Carryable))]
public class BoxContainer : MonoBehaviour
{
    [Header("Storage")]
    [SerializeField] private int capacity = 12;

    [Tooltip("Where items emerge from when unloading to a shelf.")]
    public Transform EjectPoint;

    private readonly Queue<Carryable> _items = new();
    private Carryable _selfCarryable;

    public int StoredCount => _items.Count;

    private void Awake()
    {
        _selfCarryable = GetComponent<Carryable>();
    }

    public bool TryStore(Carryable item)
    {
        if (!item) return false;
        if (item == _selfCarryable) return false;
        if (_items.Count >= capacity) return false;

        // Remove from holder/world and stick inside box
        item.DetachKeepKinematic();
        item.transform.SetParent(transform, true);

        // Optional: move inside box visually or just hide
        item.gameObject.SetActive(false);

        _items.Enqueue(item);
        return true;
    }

    /// <summary>
    /// Reactivates and returns the next item for unloading.
    /// The caller should animate it to its target (e.g., shelf slot).
    /// </summary>
    public Carryable PopItem()
    {
        if (_items.Count == 0) return null;
        var item = _items.Dequeue();
        if (!item) return null;

        item.transform.SetParent(null, true);
        item.gameObject.SetActive(true);

        // Start at eject point if provided
        if (EjectPoint)
            item.PrepareTweenFrom(EjectPoint.position, EjectPoint.rotation);
        else
            item.PrepareTweenFrom(transform.position, transform.rotation);

        return item;
    }
}
