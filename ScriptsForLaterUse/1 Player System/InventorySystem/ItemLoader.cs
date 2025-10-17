using UnityEngine;

public static class ItemLoader 
{
    public static Item LoadItem(int id)
    {
        Item item = new Item();

        // Load item from database
        ItemScript[] items = Resources.LoadAll<ItemScript>("Scripts/InventorySystem/Items");
        foreach (ItemScript i in items)
        {
            if (i.ItemID == id.ToString())
            {
                item.ItemID = i.ItemID;
                item.ItemName = i.ItemName;
                item.ShortDescription = i.ShortDescription;
                item.Description = i.Description;
                item.IconLocation = i.IconLocation;
                item.PrefabLocation = i.PrefabLocation;
                item.Quantity = i.Quantity;
                item.MaximumStack = i.MaximumStack;
                item.MaximumQuantity = i.MaximumQuantity;
                item.Class = i.Class;
                item.ForcePrefabDropQuantity = i.ForcePrefabDropQuantity;
                item.PrefabDropQuantity = i.PrefabDropQuantity;
                item.DropProperties = i.DropProperties;
                item.health = i.health;
                    item.stamina = i.stamina;
                item.attack = i.attack;
                item.defense = i.defense;
                item.magic = i.magic;
                item.magicDefense = i.magicDefense;
                item.healthRestore = i.healthRestore;
                item.staminaRestore = i.staminaRestore;
                item.attackBoost = i.attackBoost;
                item.defenseBoost = i.defenseBoost;
                item.magicBoost = i.magicBoost;
                item.magicDefenseBoost = i.magicDefenseBoost;

                item.DroppedSound = i.DroppedSound;
                item.MovedSound = i.MovedSound;
                item.UsedSound = i.UsedSound;
                item.EquippedSound = i.EquippedSound;
                item.UseDefaultSoundsIfNull = i.UseDefaultSoundsIfNull;
                
                break;
            }
        }
        
        return item;
    }
}
