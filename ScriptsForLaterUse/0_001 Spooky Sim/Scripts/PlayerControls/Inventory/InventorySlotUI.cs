using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Cherry.Inventory
{
    public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Image selectionHighlight;

        private InventoryUI _owner;
        private int _index;
        private float _lastClickTime;
        private const float DoubleClickThreshold = 0.25f;

        public void Bind(InventoryUI owner, int index)
        {
            _owner = owner; _index = index;
            Set(default);
            SetSelected(false);
        }

        public void Set(ItemStack stack)
        {
            bool has = !stack.IsEmpty;
            if (icon) { icon.enabled = has; icon.sprite = has ? stack.item.Icon : null; }
            if (countText)
            {
                if (!has || stack.amount <= 1) countText.text = string.Empty;
                else countText.text = stack.amount.ToString();
            }
        }

        public void SetSelected(bool sel)
        {
            if (selectionHighlight) selectionHighlight.enabled = sel;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                float t = Time.unscaledTime;
                if (t - _lastClickTime <= DoubleClickThreshold)
                {
                    // Double click: quick-move to first empty or try equip if equipment
                    var inv = _owner.GetInventory();
                    var s = inv.Slots[_index];
                    if (!s.IsEmpty && s.item.IsEquipment)
                    {
                        var eq = FindFirstObjectByType<EquipmentManager>();
                        if (eq != null && eq.TryEquipFromInventory(_index))
                        {
                            _owner.Refresh();
                            return;
                        }
                    }

                    // quick-move: find first empty slot (or first same item that is not full)
                    for (int i = 0; i < inv.Slots.Count; i++)
                    {
                        if (i == _index) continue;
                        if (inv.Move(_index, i)) { _owner.Refresh(); break; }
                    }
                }
                _lastClickTime = t;
                // single click select visual
                var root = transform.parent;
                foreach (Transform child in root)
                {
                    var slot = child.GetComponent<InventorySlotUI>();
                    if (slot) slot.SetSelected(slot == this);
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                // Right‑click: split half into first available spot
                var inv = _owner.GetInventory();
                var s = inv.Slots[_index];
                if (!s.IsEmpty && s.amount > 1)
                {
                    int half = Mathf.FloorToInt(s.amount / 2f);
                    for (int i = 0; i < inv.Slots.Count; i++)
                    {
                        if (i == _index) continue;
                        if (inv.Split(_index, i, half)) { _owner.Refresh(); break; }
                    }
                }
            }
        }
    }
}