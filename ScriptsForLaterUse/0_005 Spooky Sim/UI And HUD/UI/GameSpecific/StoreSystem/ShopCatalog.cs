using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopCatalog", menuName = "Game/Shop/Catalog")]
public class ShopCatalog : ScriptableObject
{
    public List<ShopItemDefinition> items = new();
}
