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

        public List<ItemDefinition> equipment;

        public ItemDefinitionHolder itemDefinitionHolder;

        private void OnUpdateItems()
        {
            SaveData.Current.mainData.playerData.playerInventory = new InventorySave();
            SaveData.Current.mainData.playerData.playerInventory.itemDefinitionsIDs = new int[slots.Count];
            SaveData.Current.mainData.playerData.playerInventory.itemAmounts = new int[slots.Count];
            SaveData.Current.mainData.playerData.playerInventory.inventoryName = "Player Inventory";
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].item != null)
                {
                    SaveData.Current.mainData.playerData.playerInventory.itemDefinitionsIDs[i] = slots[i].item.ItemId;
                    SaveData.Current.mainData.playerData.playerInventory.itemAmounts[i] = slots[i].amount;
                }
            }
            SaveData.Current.mainData.playerData.playerEquipment = new InventorySave();
            SaveData.Current.mainData.playerData.playerEquipment.itemDefinitionsIDs = new int[equipment.Count];
            SaveData.Current.mainData.playerData.playerEquipment.inventoryName = "Player Equipment";
            for (int i = 0; i < equipment.Count; i++)
            {
                SaveData.Current.mainData.playerData.playerEquipment.itemDefinitionsIDs[i] = equipment[i].ItemId;
            }
        }

        public bool TryTakeOne(int fromIndex, out ItemDefinition takenItem)
        {
            takenItem = null;
            if (fromIndex < 0 || fromIndex >= slots.Count) return false;

            var s = slots[fromIndex];
            if (s.IsEmpty) return false;

            takenItem = s.item;
            s.amount -= 1;
            if (s.amount <= 0) s.Clear();

            slots[fromIndex] = s;
            OnInventoryChanged?.Invoke();
            return true;
        }


        private void OnValidate()
        {
            slotCapacity = Mathf.Max(1, slotCapacity);
            stackLimit = Mathf.Max(1, stackLimit);
            EnsureSize(slotCapacity);
        }

        private void Start() => LoadInventory(slotCapacity);



        public int HowManyInSlot(ItemDefinition item)
        {
            foreach (var s in slots)
            {
                if (s.item == item) return s.amount;
            }
            return 0;
        }

        private void LoadInventory(int size)
        {
            EnsureSize(slotCapacity);
            LoadInventory();
            //Debug.Log("Inventory loaded with id: " + slots[0].item.ItemId + " amount: " + slots[0].amount);
            OnInventoryChanged?.Invoke();
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

        private void LoadInventory()
        {
            if (SaveData.Current.mainData.playerData.playerInventory == null) return;
            var savedInv = SaveData.Current.mainData.playerData.playerInventory;
            if (savedInv != null && savedInv.itemDefinitionsIDs != null && savedInv.itemAmounts != null)
            {
                int size = Mathf.Max(savedInv.itemDefinitionsIDs.Length, slotCapacity);
                EnsureSize(size);
                for (int i = 0; i < size; i++)
                {
                    ItemDefinition def = i < savedInv.itemDefinitionsIDs.Length ? FindItemByID(savedInv.itemDefinitionsIDs[i]) : null;
                    int amt = i < savedInv.itemAmounts.Length ? savedInv.itemAmounts[i] : 0;
                    slots[i] = new ItemStack { item = def, amount = amt };
                }
            }
            else
            {
               EnsureSize(slotCapacity);
            }

            var savedEquip = SaveData.Current.mainData.playerData.playerEquipment;
            if (savedEquip != null && savedEquip.itemDefinitionsIDs != null)
            {
                equipment = new List<ItemDefinition>();
                for (int i = 0; i < savedEquip.itemDefinitionsIDs.Length; i++)
                {
                    ItemDefinition def = FindItemByID(savedEquip.itemDefinitionsIDs[i]);
                    if (def != null)
                    {
                        equipment.Add(def);
                    }
                }
            }
            else
            {
                equipment = new List<ItemDefinition>();
            }

        }

        private ItemDefinition FindItemByID(int id)
        {
            if (itemDefinitionHolder == null) return null;
            return itemDefinitionHolder.GetItemByID(id);
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

            int x = 0;
            if (item.IsEquipment)
            {
                x = 4; // skip first 4 slots for equipment

                if (equipment.Contains(item))
                {
                    return false;
                }
                else
                {
                    equipment.Add(item);
                }



              /*  if (slots.Count <= x)
                {
                    slotCapacity = x + 1;
                    EnsureSize(slotCapacity);
                }

                for (int i = x; i < slots.Count; i++)
                {
                    var s = slots[i];
                    if (s.item == item)
                    {
                        return false; // equipment can't stack
                    }

                    if (s.IsEmpty)
                    {
                        s.item = item;
                        s.amount = 1;
                        leftover = 0;
                        slots[i] = s;
                        break;
                    }


                    if (i == slots.Count - 1)
                    {
                        Debug.Log("Expanding inventory for equipment");
                        slotCapacity += 1;
                        EnsureSize(slotCapacity);
                    }
                }*/
                return true;

            }
            else
            {
                for (int i = x; i < slots.Count && leftover > 0; i++)
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
                for (int i = x; i < slots.Count && leftover > 0; i++)
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
            }

            bool addedSomething = leftover < amount;
            OnUpdateItems();
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
            OnUpdateItems();
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
            OnUpdateItems();
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

            OnUpdateItems();
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

            OnUpdateItems();
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
            OnUpdateItems();
            return true;
        }

        public bool TryConsumeEquipment(ItemDefinition item)
        {
            if (item == null || !item.IsEquipment) return false;
            if (!equipment.Contains(item)) return false;
            equipment.Remove(item);
            Debug.Log("Removed equipment: " + item.DisplayName);
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
            OnUpdateItems();
            OnInventoryChanged?.Invoke();
            return true;
        }

        public void MarkDirty() => OnInventoryChanged?.Invoke();

        public bool IsIndexValid(int i) => i >= 0 && i < slots.Count;
    }
}