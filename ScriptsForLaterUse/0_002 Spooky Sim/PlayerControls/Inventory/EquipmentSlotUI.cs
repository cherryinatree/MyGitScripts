using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Cherry.Inventory
{
    public class EquipmentSlotUI : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text hint;

        private EquipmentSlotType _slot;

        public void Set(EquipmentSlotType slot, ItemDefinition item)
        {
            _slot = slot;
            if (label) label.text = slot.ToString();
            if (item == null)
            {
                if (icon) { icon.enabled = false; icon.sprite = null; }
                if (hint) hint.text = "(empty)";
            }
            else
            {
                if (icon) { icon.enabled = true; icon.sprite = item.Icon; }
                if (hint) hint.text = item.DisplayName;
            }
        }

        // Button hook from UI
        public void UnequipButton() => FindFirstObjectByType<EquipmentUI>()?.OnUnequip(_slot);
    }
}