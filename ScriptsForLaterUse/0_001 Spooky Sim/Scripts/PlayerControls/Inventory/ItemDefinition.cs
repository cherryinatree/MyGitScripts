using UnityEngine;

namespace Cherry.Inventory
{
    public enum ItemCategory { Generic, Consumable, Equipment }
    public enum EquipmentSlotType { None = 0, LeftHand = 1, RightHand = 2, Suit = 3, AugmentedReality = 4}

    [CreateAssetMenu(menuName = "Cherry/Inventory/Item Definition", fileName = "NewItem")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Tooltip("Stable unique id. Auto-generated if left empty.")]
        private string itemId;
        [SerializeField] private string displayName = "New Item";
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;

        [Header("Pricing")]
        [SerializeField, Min(0)] private int price = 1;

        [Header("Type & Stacking")]
        [SerializeField] private ItemCategory category = ItemCategory.Generic;
        [SerializeField, Tooltip("For equipment items only.")] private EquipmentSlotType equipSlotType = EquipmentSlotType.None;
        [SerializeField, Min(1), Tooltip("Hard cap for this item regardless of inventory stack limit.")] private int itemMaxStack = 99;

        // ------------- Public API (read-only) -------------
        public string ItemId => string.IsNullOrWhiteSpace(itemId) ? (itemId = System.Guid.NewGuid().ToString("N")) : itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public int Price => price;
        public ItemCategory Category => category;
        public EquipmentSlotType EquipSlot => equipSlotType;
        public int ItemMaxStack => Mathf.Max(1, itemMaxStack);

        public bool IsEquipment => category == ItemCategory.Equipment && equipSlotType != EquipmentSlotType.None;
    }
}