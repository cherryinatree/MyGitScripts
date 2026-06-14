// ItemPickup.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    public ItemDefinition item;
    public int quantity = 1;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var inv = other.GetComponentInParent<Inventory>();
        if (!inv) return;

        if (inv.Add(item, quantity))
            Destroy(gameObject);
        else
            Debug.Log("Inventory full.");
    }
}
