using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cherry.Inventory
{
    [Serializable]
    public struct ItemStack
    {
        public ItemDefinition item;
        [Min(0)] public int amount;
        public bool IsEmpty => item == null || amount <= 0;
        public void Clear() { item = null; amount = 0; }
    }

    public class Inventory : MonoBehaviour
    {
        [Header("Capacity & Stacking")]
        [SerializeField, Min(1)] private int slotCapacity = 20; // can be upgraded
        [SerializeField, Min(1), Tooltip("Global per-slot stack limit (respecting per-item caps). Can be upgraded.")]
        private int stackLimit = 20;

        [Header("State")]
        [SerializeField] private List<ItemStack> slots = new();

        public event Action OnInventoryChanged;

        public int SlotCapacity => slotCapacity;
        public int StackLimit => stackLimit;
        public IReadOnlyList<ItemStack> Slots => slots;

        private void OnValidate()
        {
            slotCapacity = Mathf.Max(1, slotCapacity);
            stackLimit = Mathf.Max(1, stackLimit);
            EnsureSize(slotCapacity);
        }

        private void Awake() => EnsureSize(slotCapacity);

        public int HowManyInSlot(ItemDefinition item)
        {
            foreach (var s in slots)
            {
                if (s.item == item) return s.amount;
            }
            return 0;
        }

        private void EnsureSize(int size)
        {
            if (slots == null) slots = new List<ItemStack>(size);
            if (slots.Count < size)
            {
                for (int i = slots.Count; i < size; i++) slots.Add(new ItemStack());
            }
            else if (slots.Count > size)
            {
                slots.RemoveRange(size, slots.Count - size);
            }
        }

        public void UpgradeSlots(int newCapacity)
        {
            if (newCapacity <= slotCapacity) return;
            slotCapacity = newCapacity;
            EnsureSize(slotCapacity);
            OnInventoryChanged?.Invoke();
        }

        public void UpgradeStackLimit(int newLimit)
        {
            if (newLimit <= stackLimit) return;
            stackLimit = newLimit;
            OnInventoryChanged?.Invoke();
        }

        public int GetMaxStackFor(ItemDefinition item) => Mathf.Min(stackLimit, item?.ItemMaxStack ?? 1);

        // ---- Core Ops ----
        public bool TryAddItem(ItemDefinition item, int amount, out int leftover)
        {
            leftover = amount;
            if (item == null || amount <= 0) return false;

            int perSlotCap = GetMaxStackFor(item);

            // 1) Fill existing stacks
            for (int i = 0; i < slots.Count && leftover > 0; i++)
            {
                var s = slots[i];
                if (s.item == item && s.amount < perSlotCap)
                {
                    int canTake = Mathf.Min(perSlotCap - s.amount, leftover);
                    s.amount += canTake;
                    leftover -= canTake;
                    slots[i] = s;
                }
            }

            // 2) Fill empty slots
            for (int i = 0; i < slots.Count && leftover > 0; i++)
            {
                var s = slots[i];
                if (s.IsEmpty)
                {
                    int toPlace = Mathf.Min(perSlotCap, leftover);
                    s.item = item;
                    s.amount = toPlace;
                    leftover -= toPlace;
                    slots[i] = s;
                }
            }

            bool addedSomething = leftover < amount;
            if (addedSomething) OnInventoryChanged?.Invoke();
            return addedSomething;
        }

        public bool Remove(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0) return false;
            int needed = amount;

            // Consume from stacks
            for (int i = 0; i < slots.Count && needed > 0; i++)
            {
                var s = slots[i];
                if (s.item != item) continue;
                int take = Mathf.Min(s.amount, needed);
                s.amount -= take; needed -= take;
                if (s.amount <= 0) s.Clear();
                slots[i] = s;
            }

            if (needed == 0) { OnInventoryChanged?.Invoke(); return true; }
            return false;
        }

        public int RemoveAll(ItemDefinition item)
        {
            int amount = HowManyInSlot(item);
            if (item == null || amount <= 0) return 0;
            int needed = amount;

            // Consume from stacks
            for (int i = 0; i < slots.Count && needed > 0; i++)
            {
                var s = slots[i];
                if (s.item != item) continue;
                int take = Mathf.Min(s.amount, needed);
                s.amount -= take; needed -= take;
                if (s.amount <= 0) s.Clear();
                slots[i] = s;
            }

            if (needed == 0) { OnInventoryChanged?.Invoke(); return amount; }
            return amount;
        }

        public bool Move(int from, int to)
        {
            if (!IsIndexValid(from) || !IsIndexValid(to) || from == to) return false;
            var a = slots[from];
            var b = slots[to];
            if (a.IsEmpty && b.IsEmpty) return false;

            if (!a.IsEmpty && !b.IsEmpty && a.item == b.item)
            {
                int cap = GetMaxStackFor(a.item);
                int canTake = Mathf.Min(cap - b.amount, a.amount);
                if (canTake > 0)
                {
                    b.amount += canTake; a.amount -= canTake;
                    if (a.amount <= 0) a.Clear();
                    slots[from] = a; slots[to] = b; OnInventoryChanged?.Invoke();
                    return true;
                }
            }

            // Otherwise swap
            slots[from] = b; slots[to] = a; OnInventoryChanged?.Invoke(); return true;
        }

        public bool Split(int from, int to, int amount)
        {
            if (!IsIndexValid(from) || !IsIndexValid(to) || from == to) return false;
            var src = slots[from];
            var dst = slots[to];
            if (src.IsEmpty || amount <= 0) return false;

            int cap = GetMaxStackFor(src.item);
            if (!dst.IsEmpty && dst.item != src.item) return false;

            int dstAmount = dst.IsEmpty ? 0 : dst.amount;
            int canPlace = Mathf.Min(cap - dstAmount, amount, src.amount);
            if (canPlace <= 0) return false;

            // apply
            src.amount -= canPlace;
            if (dst.IsEmpty) dst.item = src.item;
            dst.amount = dstAmount + canPlace;
            if (src.amount <= 0) src.Clear();

            slots[from] = src; slots[to] = dst; OnInventoryChanged?.Invoke();
            return true;
        }

        public bool ClearSlot(int index)
        {
            if (!IsIndexValid(index)) return false;
            if (slots[index].IsEmpty) return false;
            var s = slots[index];
            s.Clear();
            slots[index] = s;
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool TryConsumeAtIndex(int index, int amount)
        {
            if (!IsIndexValid(index) || amount <= 0) return false;
            var s = slots[index];
            if (s.IsEmpty || s.amount < amount) return false;
            s.amount -= amount;
            if (s.amount <= 0) s.Clear();
            slots[index] = s;
            OnInventoryChanged?.Invoke();
            return true;
        }

        public void MarkDirty() => OnInventoryChanged?.Invoke();

        public bool IsIndexValid(int i) => i >= 0 && i < slots.Count;
    }
}