using System.Collections.Generic;
using UnityEngine;

namespace Cherry.Inventory
{
    public class EquipmentUI : MonoBehaviour
    {
        [System.Serializable]
        public class SlotBinding
        {
            public EquipmentSlotType slot;
            public EquipmentSlotUI widget;
        }

        [SerializeField] private EquipmentManager equipment;
        [SerializeField] private List<SlotBinding> slots;

        private void OnEnable()
        {
            if (equipment) equipment.OnEquipmentChanged += Refresh;
            Refresh();
        }
        private void OnDisable()
        {
            if (equipment) equipment.OnEquipmentChanged -= Refresh;
        }

        public void Refresh()
        {
            if (equipment == null) return;
            foreach (var b in slots)
            {
                var item = equipment.GetEquipped(b.slot);
                b.widget.Set(b.slot, item);
            }
        }

        public void OnUnequip(EquipmentSlotType slot)
        {
            if (equipment.UnequipToInventory(slot)) Refresh();
        }
    }
}