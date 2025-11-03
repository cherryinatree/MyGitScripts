using Cherry.Inventory;
using UnityEngine;
public interface IInventoryOps
{
    int Count(ItemDefinition item);
    int RemoveAll(ItemDefinition item);     // returns how many removed
    bool Add(ItemDefinition item, int amt); // (not used here, but handy)
}

[AddComponentMenu("Inventory/Inventory Ops Adapter")]
public class InventoryOpsAdapter : MonoBehaviour, IInventoryOps
{
    [SerializeField] public Inventory inventory; // drag your Inventory component here

    // EDIT these to call your actual methods (or just rename your Inventory to match)
    public int Count(ItemDefinition item)
    {
        return inventory.HowManyInSlot(item);
    }

    public int RemoveAll(ItemDefinition item)
        => (int)inventory.GetType().GetMethod("RemoveAll").Invoke(inventory, new object[] { item });

    public bool Add(ItemDefinition item, int amt)
    {
        var m = inventory.GetType().GetMethod("Add");
        if (m == null) m = inventory.GetType().GetMethod("AddItem");
        var result = m?.Invoke(inventory, new object[] { item, amt });
        return result is bool b ? b : true;
    }
}
