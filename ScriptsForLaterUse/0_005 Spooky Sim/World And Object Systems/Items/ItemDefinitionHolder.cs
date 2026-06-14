using UnityEngine;

namespace Cherry.Inventory
{
    [CreateAssetMenu(menuName = "Cherry/Inventory/Item Definition Holder", fileName = "NewItemHolder")]
    public class ItemDefinitionHolder : ScriptableObject
    {
        public ItemDefinition[] itemDefinition;

        public ItemDefinition GetItemByID(int id)
        {
            foreach (ItemDefinition item in itemDefinition)
            {
                if (item.ItemId == id)
                {
                    return item;
                }
            }
            return null;
        }
    }
}