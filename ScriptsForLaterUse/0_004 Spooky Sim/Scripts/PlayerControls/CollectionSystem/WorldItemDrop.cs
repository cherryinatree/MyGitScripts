using UnityEngine;

namespace Cherry.Inventory
{
    [AddComponentMenu("Cherry/Inventory/World Item Drop")]
    public class WorldItemDrop : MonoBehaviour
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int amount = 1;

        public ItemDefinition Item => item;
        public int Amount => amount;

        public void Set(ItemDefinition itemDef, int amt)
        {
            item = itemDef;
            amount = Mathf.Max(1, amt);
        }

        public bool TryPeek(out ItemDefinition itemDef, out int amt)
        {
            itemDef = item;
            amt = amount;
            return itemDef != null && amt > 0;
        }

        /// <summary>Consumes some amount. Returns true if now empty (safe to Destroy).</summary>
        public bool Consume(int amtUsed)
        {
            if (amtUsed <= 0) return false;
            amount -= amtUsed;
            if (amount <= 0)
            {
                amount = 0;
                item = null;
                return true;
            }
            return false;
        }
    }
}
