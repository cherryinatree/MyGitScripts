// CharacterInventoryNet.cs
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CharacterInventoryNet : NetworkBehaviour
{
    [Header("Config")]
    public ItemDatabase db;
    public int slotLimit = 24;

    [Header("State")]
    public NetworkList<NetworkItemStack> contents;
    public NetworkVariable<int> credits = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
    {
        contents = new NetworkList<NetworkItemStack>();
    }

    public override void OnNetworkSpawn()
    {
        if (db) db.Init();
    }

    // ===== Server-side inventory logic =====
    bool CanAddServer(string itemId, int qty)
    {
        var def = db.Get(itemId);
        if (!def || qty <= 0) return false;

        if (def.stackable)
        {
            int needed = qty;
            int freeSlots = slotLimit - contents.Count;
            // First pass: fill existing stacks
            foreach (var st in contents)
            {
                if (st.itemId.ToString() != itemId) continue;
                int space = def.maxStack - st.quantity;
                if (space > 0)
                {
                    int take = Mathf.Min(space, needed);
                    needed -= take;
                    if (needed <= 0) return true;
                }
            }
            // Remaining requires new stacks
            int stacksNeeded = Mathf.CeilToInt(needed / (float)def.maxStack);
            return stacksNeeded <= freeSlots;
        }
        else
        {
            int freeSlots = slotLimit - contents.Count;
            return qty <= freeSlots;
        }
    }

    void AddServer(string itemId, int qty)
    {
        var def = db.Get(itemId);
        if (!def) return;
        Debug.Log("Def");

        if (def.stackable)
        {
            // Fill existing
            for (int i = 0; i < contents.Count && qty > 0; i++)
            {
                var st = contents[i];
                if (st.itemId.ToString() != itemId) continue;
                int space = def.maxStack - st.quantity;
                if (space <= 0) continue;
                int take = Mathf.Min(space, qty);
                st.quantity += take;
                contents[i] = st;
                qty -= take;
            }
            // New stacks
            while (qty > 0)
            {
                int put = Mathf.Min(def.maxStack, qty);
                contents.Add(new NetworkItemStack(itemId, put));
                qty -= put;
            }
        }
        else
        {
            for (int n = 0; n < qty; n++)
                contents.Add(new NetworkItemStack(itemId, 1));
        }
    }

    bool RemoveServer(string itemId, int qty)
    {
        if (qty <= 0) return false;
        int have = CountServer(itemId);
        if (have < qty) return false;

        for (int i = contents.Count - 1; i >= 0 && qty > 0; i--)
        {
            var st = contents[i];
            if (st.itemId.ToString() != itemId) continue;
            int take = Mathf.Min(st.quantity, qty);
            st.quantity -= take;
            qty -= take;
            if (st.quantity <= 0) contents.RemoveAt(i);
            else contents[i] = st;
        }
        return true;
    }

    int CountServer(string itemId)
    {
        int total = 0;
        foreach (var st in contents)
            if (st.itemId.ToString() == itemId) total += st.quantity;
        return total;
    }

    // ===== Public client helpers (local read mirrors) =====
    public int CountLocal(string itemId) => CountServer(itemId); // same data locally

    // ===== RPCs =====

    [ServerRpc(RequireOwnership = false)]
    public void RequestAddItemServerRpc(string itemId, int qty)
    {
        if (!CanAddServer(itemId, qty)) return;
        Debug.Log("AddServer");
        AddServer(itemId, qty);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestRemoveItemServerRpc(string itemId, int qty)
    {
        RemoveServer(itemId, qty);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSpendCreditsServerRpc(int amount)
    {
        if (amount <= 0) return;
        if (credits.Value >= amount) credits.Value -= amount;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestAddCreditsServerRpc(int amount)
    {
        if (amount > 0) credits.Value += amount;
    }

    // Convenience (server only)
    public bool TryServerBuy(string itemId, int qty, int priceEach)
    {
        long total = (long)qty * priceEach;
        if (total > int.MaxValue) return false;
        if (credits.Value < (int)total) return false;
        if (!CanAddServer(itemId, qty)) return false;

        credits.Value -= (int)total;
        AddServer(itemId, qty);
        return true;
    }
}
