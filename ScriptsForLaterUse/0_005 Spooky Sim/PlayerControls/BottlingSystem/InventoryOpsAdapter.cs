using Cherry.Inventory;
using UnityEngine;
public interface IInventoryOps
{
    int Count(ItemDefinition item);
    int RemoveAll(ItemDefinition item);     // returns how many removed
    bool Add(ItemDefinition item, int amt); // (not used here, but handy)


    bool TryRemove(ItemDefinition item, int amount);
    bool TryAdd(ItemDefinition item, int amount);
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

    public bool TryRemove(ItemDefinition item, int amount)
    {
        //var m = inventory.Remove(item, amount);
       // if (m == null) m = inventory.GetType().GetMethod("RemoveItem");
       // var result = m?.Invoke(inventory, new object[] { item, amount });
       // return result is bool b && b;
       return inventory.Remove(item, amount);
    }

    public bool TryAdd(ItemDefinition item, int amount)
    {
        /*var m = inventory.GetType().GetMethod("TryAdd");
        if (m == null) m = inventory.GetType().GetMethod("AddItem");
        var result = m?.Invoke(inventory, new object[] { item, amount });
        return result is bool b && b;*/
        bool result = inventory.TryAddItem(item, amount, out int leftover);
        return result;
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
