using Cherry.Inventory;
using UnityEngine;
public interface IInventorySink
{
    /// <summary>Return true if the item was accepted.</summary>
    bool AddItem(ItemDefinition item, int amount);
}

[AddComponentMenu("Inventory/Inventory Sink Adapter")]
public class InventorySinkAdapter : MonoBehaviour, IInventorySink
{
    [SerializeField] private Inventory inventory; // assign your Inventory component here

    public bool AddItem(ItemDefinition item, int amount)
    {
        if (inventory == null)
        {
            Debug.LogWarning($"{name}: No inventoryBehaviour assigned.");
            return false;
        }
        int leftover = 0;
        return inventory.TryAddItem(item, amount, out leftover);

        // JOHN READ THIS!
        // should take the leftover and instantiate dropped items in the world if needed




        /*
        // Try common method names. Adjust to your API if different.
        var invType = inventoryBehaviour.GetType();
        var m = invType.GetMethod("Add") ?? invType.GetMethod("AddItem") ?? invType.GetMethod("TryAdd");
        if (m == null)
        {
            Debug.LogError($"{name}: inventoryBehaviour has no Add/AddItem/TryAdd method.");
            return false;
        }

        object result = m.Invoke(inventoryBehaviour, new object[] { item, amount });
        return result is bool b ? b : true; // assume success if method is void*/
    }
}
