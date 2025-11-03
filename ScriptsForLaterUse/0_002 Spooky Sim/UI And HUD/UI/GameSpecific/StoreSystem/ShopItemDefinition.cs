using UnityEngine;

public enum ShopCategory { Furniture, Fixtures, Decor, Consumable, Misc, Upgrades }

[CreateAssetMenu(fileName = "ShopItem", menuName = "Game/Shop/Item")]
public class ShopItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public string itemId;                          // unique, stable id
    public string displayName = "New Item";
    [TextArea] public string description;
    public Sprite icon;
    public ShopCategory category = ShopCategory.Misc;

    [Header("Storefront")]
    [Min(0)] public int price = 10;
    [Min(0)] public int levelRequired = 0;

    [Header("Product (spawned)")]
    public GameObject productPrefab;               // for kind=Product

}
