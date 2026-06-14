using UnityEngine;
using Cherry.Inventory;

namespace Cherry.Bosses
{
    [AddComponentMenu("Cherry/Bosses/Pickup Item Assigner")]
    public class PickupItemAssigner : MonoBehaviour
    {
        [SerializeField] private ItemDefinition item;

        // Call from boss on death
        public void SetItem(ItemDefinition newItem) => item = newItem;

        // Your pickup logic likely already exists elsewhere.
        // This script is only meant to carry/assign the ItemDefinition cleanly.
        public ItemDefinition GetItem() => item;
    }
}