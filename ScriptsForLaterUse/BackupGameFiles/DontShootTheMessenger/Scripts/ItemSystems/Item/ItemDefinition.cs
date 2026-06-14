// ItemDefinition.cs
using UnityEngine;

public enum ItemCategory { Weapon, Ammo, Armor, Defense, Supply, Decoration, Cosmetic, Utility }
public enum EquipSlot { None, PrimaryWeapon, SecondaryWeapon, Head, Body }

[CreateAssetMenu(menuName = "Game/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public string itemId;               // unique (string is designer-friendly)
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemCategory category;

    [Header("Inventory")]
    public bool stackable = true;
    public int maxStack = 99;

    [Header("World Prefabs")]
    public GameObject pickupPrefab;     // world pickup model
    public GameObject equippedPrefab;   // if Weapon/Armor, prefab to attach when equipped
    public GameObject placeablePrefab;  // if placeable (decor, bench, turret)

    [Header("Equip / Place")]
    public EquipSlot equipSlot = EquipSlot.None;
    public bool isPlaceable;            // decorations, utilities, defenses
    public Vector3 placeOffset;         // small offset from ground
    public bool snapToGrid = true;
    public float gridSize = 0.5f;

    [Header("Shop")]
    public int baseBuyPrice = 100;
    public int baseSellPrice = 50;      // optional; 0 to disable selling
}
