using System;
using UnityEngine;

namespace Cherry.Inventory
{
    public class EquipmentManager : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;

        // We store equipment in a simple array indexed by (int)slotType.
        // Index 0 (None) is unused; array length = highest enum value + 1.
        [SerializeField] private ItemDefinition[] equipped;

        public event Action OnEquipmentChanged;

        private void Awake()
        {
            if (equipped == null || equipped.Length == 0)
                equipped = new ItemDefinition[Enum.GetValues(typeof(EquipmentSlotType)).Length];
        }

        public ItemDefinition GetEquipped(EquipmentSlotType slot) => equipped[(int)slot];

        public bool TryEquipFromInventory(int invIndex)
        {
            if (inventory == null || !inventory.IsIndexValid(invIndex)) return false;
            var s = inventory.Slots[invIndex];
            if (s.IsEmpty || !s.item.IsEquipment) return false;
            return TryEquip(invIndex, s.item);
        }

        public bool TryEquip(int fromInventoryIndex, ItemDefinition item)
        {
            if (!item.IsEquipment) return false;
            var slot = item.EquipSlot;
            var cur = equipped[(int)slot];

            // "Unequip" current into inventory if possible
            if (cur != null)
            {
                if (!inventory.TryAddItem(cur, 1, out int leftover) || leftover > 0)
                    return false; // no space to swap
            }

            // Remove one of the item from inventory and equip it
            if (!inventory.TryConsumeAtIndex(fromInventoryIndex, 1)) return false;

            equipped[(int)slot] = item;
            inventory.MarkDirty();
            OnEquipmentChanged?.Invoke();
            return true;
        }

        public bool UnequipToInventory(EquipmentSlotType slot)
        {
            var cur = equipped[(int)slot];
            if (cur == null) return false;
            if (!inventory.TryAddItem(cur, 1, out int leftover) || leftover > 0) return false;
            equipped[(int)slot] = null;
            OnEquipmentChanged?.Invoke();
            return true;
        }
    }
}