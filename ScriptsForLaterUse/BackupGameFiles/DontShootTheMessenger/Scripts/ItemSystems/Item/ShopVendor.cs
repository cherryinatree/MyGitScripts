// ShopVendor.cs
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShopStockEntry
{
    public ItemDefinition item;
    public int priceOverride = -1; // -1 = use item.baseBuyPrice
    public int quantity = -1;      // -1 = infinite
}

public class ShopVendor : MonoBehaviour
{
    public string shopName = "Base Quartermaster";
    public List<ShopStockEntry> stock = new();

    public bool TryBuy(CharacterInventory player, ItemDefinition item, int qty = 1)
    {
        var entry = stock.Find(e => e.item == item);
        if (entry == null) return false;
        if (entry.quantity == 0) return false;
        int priceEach = entry.priceOverride >= 0 ? entry.priceOverride : item.baseBuyPrice;
        int total = priceEach * qty;

        if (!player.wallet.TrySpend(total)) return false;
        if (!player.inventory.Add(item, qty))
        {
            // refund if inventory full
            player.wallet.Add(total);
            return false;
        }
        if (entry.quantity > 0) entry.quantity = Mathf.Max(0, entry.quantity - qty);
        return true;
    }

    public bool TrySell(CharacterInventory player, ItemDefinition item, int qty = 1)
    {
        if (item.baseSellPrice <= 0) return false;
        if (!player.inventory.Remove(item, qty)) return false;
        player.wallet.Add(item.baseSellPrice * qty);
        return true;
    }
}
