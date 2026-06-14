using System;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cherry.Inventory
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Inventory")]
        [SerializeField] private Inventory inventory;

        [Header("Slots in Scene (no instantiation)")]
        [SerializeField] private Transform gridRoot; // parent holding InventorySlotUI children
        [Tooltip("Optional. If empty, slots are auto-collected from GridRoot children (by hierarchy order).")]
        [SerializeField] private List<InventorySlotUI> sceneSlots = new();

        [Header("Selection")]
        [SerializeField] private bool enableScrollWheelSelection = true;
        [SerializeField] private bool invertScroll = false;
        [SerializeField] private bool wrapSelection = true;

        [Tooltip("If true, selection will only move among active slots (<= inventory capacity and <= provided slot count).")]
        [SerializeField] private bool clampToActiveSlots = true;

        [Header("Highlight Colors")]
        [SerializeField] private Color selectedHighlightColor = new Color(1f, 0.85f, 0.1f, 1f);
        [SerializeField] private Color unselectedHighlightColor = new Color(1f, 1f, 1f, 0.2f);

        private readonly List<InventorySlotUI> _slotUIs = new();
        private int _selectedIndex = 0;

        public int SelectedIndex => _selectedIndex;
        public event Action<int> OnSelectionChanged;

        private void OnEnable()
        {
            if (inventory != null)
                inventory.OnInventoryChanged += Refresh;

            BuildFromScene();
            Refresh();
            ClampAndApplySelectionVisuals(forceEvent: false);
        }

        private void OnDisable()
        {
            if (inventory != null)
                inventory.OnInventoryChanged -= Refresh;
        }

        private void Update()
        {
            if (!enableScrollWheelSelection) return;

            float scroll = ReadScrollY();
            if (Mathf.Abs(scroll) < 0.0001f) return;

            if (invertScroll) scroll *= -1f;

            if (scroll > 0f) SelectNext();
            else SelectPrev();
        }

        private void BuildFromScene()
        {
            _slotUIs.Clear();

            // If user manually assigned slots, use those
            if (sceneSlots != null && sceneSlots.Count > 0)
            {
                for (int i = 0; i < sceneSlots.Count; i++)
                {
                    if (sceneSlots[i] != null)
                        _slotUIs.Add(sceneSlots[i]);
                }
            }
            else
            {
                // Otherwise, auto-collect from children under gridRoot
                if (gridRoot == null) return;

                var found = gridRoot.GetComponentsInChildren<InventorySlotUI>(includeInactive: true);
                Array.Sort(found, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

                for (int i = 0; i < found.Length; i++)
                    _slotUIs.Add(found[i]);
            }

            // Bind indices to whatever we found
            for (int i = 0; i < _slotUIs.Count; i++)
                _slotUIs[i].Bind(this, i);
        }

        public void RebuildIfCapacityChanged()
        {
            // We can't create more slots, but we can activate/deactivate based on capacity.
            ApplyCapacityActiveState();
        }

        public void Refresh()
        {
            if (inventory == null) return;

            RebuildIfCapacityChanged();

            var slots = inventory.Slots;
            int countToShow = Mathf.Min(_slotUIs.Count, slots.Count);

            // Set data for visible range
            for (int i = 0; i < countToShow; i++)
                _slotUIs[i].Set(slots[i]);

            // Clear any extra UI slots if you have more UI objects than inventory slots
            for (int i = countToShow; i < _slotUIs.Count; i++)
                _slotUIs[i].Set(default);

            ClampAndApplySelectionVisuals(forceEvent: false);
        }

        private void ApplyCapacityActiveState()
        {
            if (inventory == null) return;

            int needed = inventory.SlotCapacity;
            int available = _slotUIs.Count;

            if (needed > available)
            {
                Debug.LogWarning(
                    $"{nameof(InventoryUI)}: Inventory capacity ({needed}) is larger than provided UI slots ({available}). " +
                    $"Only the first {available} slots will be shown.",
                    this
                );
            }

            for (int i = 0; i < _slotUIs.Count; i++)
                _slotUIs[i].gameObject.SetActive(i < needed);
        }

        // Called by a slot (click) or other UI to set selection
        public void SelectIndex(int index)
        {
            int max = GetSelectableSlotCount();
            if (max <= 0) return;

            index = WrapOrClamp(index, max);
            if (_selectedIndex == index) return;

            _selectedIndex = index;
            ApplySelectionVisuals();

            OnSelectionChanged?.Invoke(_selectedIndex);
        }

        public void SelectNext() => SelectIndex(_selectedIndex + 1);
        public void SelectPrev() => SelectIndex(_selectedIndex - 1);

        // Optional: slot can call this on click
        public void OnSlotClicked(int index) => SelectIndex(index);

        private void ClampAndApplySelectionVisuals(bool forceEvent)
        {
            int max = GetSelectableSlotCount();
            if (max <= 0)
            {
                _selectedIndex = 0;
                ApplySelectionVisuals();
                return;
            }

            int clamped = WrapOrClamp(_selectedIndex, max);
            bool changed = clamped != _selectedIndex;

            _selectedIndex = clamped;
            ApplySelectionVisuals();

            if ((changed && forceEvent) || (!changed && forceEvent))
                OnSelectionChanged?.Invoke(_selectedIndex);
        }

        private void ApplySelectionVisuals()
        {
            int max = GetSelectableSlotCount();
            for (int i = 0; i < _slotUIs.Count; i++)
            {
                bool active = _slotUIs[i].gameObject.activeInHierarchy;
                bool selectable = (!clampToActiveSlots) || active;

                bool isSelected = selectable && (i == _selectedIndex);
                _slotUIs[i].SetSelected(isSelected, selectedHighlightColor, unselectedHighlightColor);
            }
        }

        private int GetSelectableSlotCount()
        {
            if (_slotUIs.Count == 0) return 0;

            if (!clampToActiveSlots) return _slotUIs.Count;

            // only active slots, usually <= capacity
            int count = 0;
            for (int i = 0; i < _slotUIs.Count; i++)
                if (_slotUIs[i] != null && _slotUIs[i].gameObject.activeInHierarchy)
                    count++;

            return count;
        }

        private int WrapOrClamp(int index, int maxExclusive)
        {
            if (maxExclusive <= 0) return 0;

            if (wrapSelection)
            {
                index %= maxExclusive;
                if (index < 0) index += maxExclusive;
                return index;
            }

            return Mathf.Clamp(index, 0, maxExclusive - 1);
        }

        private float ReadScrollY()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse == null) return 0f;
            return mouse.scroll.ReadValue().y; // typically +/- 120 per notch
#else
            return Input.mouseScrollDelta.y;
#endif
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
