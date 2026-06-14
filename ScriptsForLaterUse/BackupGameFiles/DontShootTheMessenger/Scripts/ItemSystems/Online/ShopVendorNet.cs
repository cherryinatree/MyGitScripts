// ShopVendorNet.cs
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class ShopVendorNet : NetworkBehaviour
{
    [System.Serializable]
    public class StockEntry
    {
        [Tooltip("Must match ItemDefinition.itemId in ItemDatabase")]
        public string itemId;

        [Tooltip("-1 = use ItemDefinition.baseBuyPrice")]
        public int priceEach = -1;

        [Tooltip("-1 = infinite, 0 = out of stock, >0 = remaining unique purchases")]
        public int quantity = -1;

        [Tooltip("How many units the spawned pickup will represent per Buy click")]
        public int spawnQuantity = 1;
    }

    [Header("Database & Stock")]
    public ItemDatabase db;                // assign or auto-load (see EnsureDb)
    public List<StockEntry> stock = new(); // define in Inspector by itemId

    [Header("Pickup Spawning")]
    public Transform pickupSpawn;          // where purchased items appear
    [Tooltip("Used if ItemDefinition has no pickupPrefab. Must have NetworkObject + NetItemPickup.")]
    public GameObject fallbackPickupPrefab;

    void EnsureDb()
    {
        if (!db)
            db = Resources.Load<ItemDatabase>("ItemDatabase");
        if (!db)
        {
            Debug.LogError("[ShopVendorNet] ItemDatabase not assigned or not in Resources/ItemDatabase");
            return;
        }
        db.Init();
    }

    StockEntry Find(string id) => stock.Find(e => e.itemId == id);

    [ServerRpc(RequireOwnership = false)]
    public void RequestBuyServerRpc(NetworkObjectReference buyerRef, string itemId, int qty)
    {
        if (qty <= 0) return;
        EnsureDb();

        if (!buyerRef.TryGet(out var buyerNO)) return;
        var buyerInv = buyerNO.GetComponent<CharacterInventoryNet>();
        if (!buyerInv) return;

        var entry = Find(itemId);
        if (entry == null) return;
        if (entry.quantity == 0) return; // out of stock

        var def = db.Get(itemId);
        int price = entry.priceEach >= 0 ? entry.priceEach : (def ? def.baseBuyPrice : 0);
        if (price < 0) price = 0;

        long total = (long)price * qty;
        if (total > int.MaxValue) return;

        if (buyerInv.credits.Value < (int)total) return; // not enough funds

        // Deduct funds
        buyerInv.credits.Value -= (int)total;

        // Optionally decrement stock
        if (entry.quantity > 0)
            entry.quantity = Mathf.Max(0, entry.quantity - qty);

        // Spawn pickups at the vendor spawn
        Vector3 basePos = pickupSpawn ? pickupSpawn.position : (transform.position + transform.forward * 1.0f);
        Quaternion rot = pickupSpawn ? pickupSpawn.rotation : transform.rotation;

        for (int i = 0; i < qty; i++)
        {
            SpawnPickup(def, basePos + new Vector3(0.25f * i, 0f, 0f), rot, entry.spawnQuantity, itemId);
        }
    }

    void SpawnPickup(ItemDefinition def, Vector3 pos, Quaternion rot, int quantity, string fallbackItemId)
    {
        EnsureDb();
        string id = def ? def.itemId : fallbackItemId;

        GameObject prefab = def && def.pickupPrefab ? def.pickupPrefab : fallbackPickupPrefab;
        if (!prefab)
        {
            Debug.LogError("[ShopVendorNet] No pickupPrefab on ItemDefinition and no fallbackPickupPrefab assigned.");
            return;
        }

        var go = Instantiate(prefab, pos, rot);
        var no = go.GetComponent<NetworkObject>() ?? go.AddComponent<NetworkObject>();
        var pickup = go.GetComponent<NetItemPickup>() ?? go.AddComponent<NetItemPickup>();

        pickup.itemId = id;
        pickup.quantity = Mathf.Max(1, quantity);

        no.Spawn(true);
    }
}
