// Inventory.cs
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Header("Inventory")]
    public int slots = 24;
    public List<ItemStack> contents = new();

    public System.Action OnChanged;

    public bool CanAdd(ItemDefinition item, int qty = 1)
    {
        // can merge into existing stacks?
        if (item.stackable)
        {
            foreach (var st in contents)
                if (st.item == item && st.quantity + qty <= item.maxStack)
                    return true;
        }
        // empty slot?
        int used = contents.Count;
        // count stacks that are already full or non-matching
        bool needsNewStack = !item.stackable || !HasMergeRoom(item, qty);
        if (needsNewStack && used < slots) return true;

        return !needsNewStack; // if we could merge, then it's ok
    }

    bool HasMergeRoom(ItemDefinition item, int qty)
    {
        foreach (var st in contents)
        {
            if (st.item == item)
            {
                int canTake = st.item.maxStack - st.quantity;
                if (canTake >= qty) return true;
                qty -= canTake;
            }
        }
        return qty <= 0;
    }

    public bool Add(ItemDefinition item, int qty = 1)
    {
        if (!CanAdd(item, qty)) return false;

        if (item.stackable)
        {
            // fill existing stacks first
            foreach (var st in contents)
            {
                if (st.item == item && st.quantity < item.maxStack)
                {
                    int canTake = Mathf.Min(item.maxStack - st.quantity, qty);
                    st.quantity += canTake;
                    qty -= canTake;
                    if (qty == 0) { OnChanged?.Invoke(); return true; }
                }
            }
        }
        // make new stacks for remainder
        while (qty > 0)
        {
            int put = item.stackable ? Mathf.Min(item.maxStack, qty) : 1;
            contents.Add(new ItemStack(item, put));
            qty -= put;
        }
        OnChanged?.Invoke();
        return true;
    }

    public bool Remove(ItemDefinition item, int qty = 1)
    {
        for (int i = contents.Count - 1; i >= 0 && qty > 0; i--)
        {
            var st = contents[i];
            if (st.item != item) continue;
            int take = Mathf.Min(st.quantity, qty);
            st.quantity -= take;
            qty -= take;
            if (st.quantity <= 0) contents.RemoveAt(i);
        }
        bool ok = qty == 0;
        if (ok) OnChanged?.Invoke();
        return ok;
    }

    public int Count(ItemDefinition item)
    {
        int total = 0;
        foreach (var st in contents) if (st.item == item) total += st.quantity;
        return total;
    }
}
