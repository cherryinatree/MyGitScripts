using Cherry.Inventory;
using UnityEngine;

public class AddEquipment : MonoBehaviour
{

    public ItemDefinition equipmentToAdd;
    Inventory inventory;
    EquipmentManager equipmentManager;

    private void Start()
    {
        inventory = FindFirstObjectByType<Inventory>();
        equipmentManager = FindFirstObjectByType<EquipmentManager>();
    }
    
    public void AddEquipmentToInventory()
    {
        if (equipmentToAdd != null && inventory != null)
        {
            inventory.TryAddItem(equipmentToAdd, 1, out int leftover);
            //equipmentManager.TryEquip(inventory.equipment.Count-1, equipmentToAdd);
            Destroy(gameObject);
        }
    }
}
