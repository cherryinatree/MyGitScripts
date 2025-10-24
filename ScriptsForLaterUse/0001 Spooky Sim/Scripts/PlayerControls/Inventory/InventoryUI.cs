using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cherry.Inventory
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;
        [SerializeField] private Transform gridRoot; // parent with GridLayoutGroup
        [SerializeField] private GameObject slotPrefab; // prefab with InventorySlotUI

        private readonly List<InventorySlotUI> _slotUIs = new();

        private void OnEnable()
        {
            if (inventory != null)
                inventory.OnInventoryChanged += Refresh;
            Build();
            Refresh();
        }
        private void OnDisable()
        {
            if (inventory != null)
                inventory.OnInventoryChanged -= Refresh;
        }

        private void Build()
        {
            if (inventory == null || gridRoot == null || slotPrefab == null) return;
            int needed = inventory.SlotCapacity;
            while (_slotUIs.Count < needed)
            {
                var go = Instantiate(slotPrefab, gridRoot);
                var ui = go.GetComponent<InventorySlotUI>();
                ui.Bind(this, _slotUIs.Count);
                _slotUIs.Add(ui);
            }
            for (int i = 0; i < _slotUIs.Count; i++)
            {
                _slotUIs[i].gameObject.SetActive(i < needed);
                _slotUIs[i].Bind(this, i);
            }
        }

        public void RebuildIfCapacityChanged()
        {
            if (_slotUIs.Count != inventory.SlotCapacity) { Build(); }
        }

        public void Refresh()
        {
            if (inventory == null) return;
            RebuildIfCapacityChanged();
            var slots = inventory.Slots;
            for (int i = 0; i < _slotUIs.Count; i++)
            {
                _slotUIs[i].Set(slots[i]);
            }
        }

        // Simple actions for UI buttons / clicks
        public void OnMoveRequest(int fromIndex, int toIndex)
        {
            if (inventory.Move(fromIndex, toIndex)) Refresh();
        }

        public void OnSplitHalfRequest(int fromIndex, int toIndex)
        {
            var s = inventory.Slots[fromIndex];
            int half = Mathf.CeilToInt(s.amount * 0.5f);
            if (half > 0 && inventory.Split(fromIndex, toIndex, half)) Refresh();
        }

        public Inventory GetInventory() => inventory;
    }
}