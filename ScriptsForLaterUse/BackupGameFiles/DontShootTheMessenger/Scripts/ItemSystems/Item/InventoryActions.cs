// InventoryActions.cs
using UnityEngine;

public class InventoryActions : MonoBehaviour
{
    public Inventory inventory;
    public EquipmentSystem equipment;

    public bool EquipItem(ItemDefinition item)
    {
        if (equipment.Equip(item))
        {
            // If equipment consumes the physical item, remove 1
            if (inventory.Remove(item, 1)) return true;
        }
        return false;
    }
}
