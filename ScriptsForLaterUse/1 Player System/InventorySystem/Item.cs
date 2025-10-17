using MoreMountains.InventoryEngine;
using MoreMountains.Tools;
using UnityEngine;
using static ItemScript;

[System.Serializable]
public class Item 
{
    [Tooltip("the (unique) ID of the item")]
    public string ItemID;

    public int Quantity = 1;

    [Header("Basic info")]
    /// the name of the item - will be displayed in the details panel
    [Tooltip("the name of the item - will be displayed in the details panel")]
    public string ItemName;
    /// the item's short description to display in the details panel
    [TextArea]
    [Tooltip("the item's short description to display in the details panel")]
    public string ShortDescription;
    [TextArea]
    /// the item's long description to display in the details panel
    [Tooltip("the item's long description to display in the details panel")]
    public string Description;

    [Header("Image")]
    /// the icon that will be shown on the inventory's slot
    [Tooltip("the icon that will be shown on the inventory's slot")]
    public string IconLocation;

    [Header("Prefab Drop")]
    /// the prefab to instantiate when the item is dropped
    [Tooltip("the prefab to instantiate when the item is dropped")]
    public string PrefabLocation;
    /// if this is true, the quantity of the object will be forced to PrefabDropQuantity when dropped
    [Tooltip("if this is true, the quantity of the object will be forced to PrefabDropQuantity when dropped")]
    public bool ForcePrefabDropQuantity = false;
    /// the quantity to force on the spawned item if ForcePrefabDropQuantity is true
    [Tooltip("the quantity to force on the spawned item if ForcePrefabDropQuantity is true")]
    [MMCondition("ForcePrefabDropQuantity", true)]
    public int PrefabDropQuantity = 1;
    /// the minimal distance at which the object should be spawned when dropped
    [Tooltip("the minimal distance at which the object should be spawned when dropped")]
    public MMSpawnAroundProperties DropProperties;

    [Header("Inventory Properties")]
    /// If this object can be stacked (multiple instances in a single inventory slot), you can specify here the maximum size of that stack.
    [Tooltip("If this object can be stacked (multiple instances in a single inventory slot), you can specify here the maximum size of that stack.")]
    public int MaximumStack = 1;
    /// the maximum quantity allowed of this item in the target inventory
    [Tooltip("the maximum quantity allowed of this item in the target inventory")]
    public int MaximumQuantity = 999999999;
    /// the class of the item
    [Tooltip("the class of the item")]

    public ItemClass Class;


    [Header("Effects for equipment")]
    public int health = 0;
    public int stamina = 0;
    public int attack = 0;
    public int defense = 0;
    public int magic = 0;
    public int magicDefense = 0;


    [Header("Effects for Consumables")]
    public int healthRestore = 0;
    public int staminaRestore = 0;
    public int attackBoost = 0;
    public int defenseBoost = 0;
    public int magicBoost = 0;
    public int magicDefenseBoost = 0;

    /// the sound the item should play when equipped (optional)
    [Tooltip("the sound the item should play when equipped (optional)")]
    public AudioClip EquippedSound;

    [Header("Usable")]
    /// If this item can be used, you can set here a sound to play when it gets used, if you don't a default sound will be played.
    [Tooltip("If this item can be used, you can set here a sound to play when it gets used, if you don't a default sound will be played.")]
    public AudioClip UsedSound;

    [Header("Sounds")]
    /// the sound the item should play when moved (optional)
    [Tooltip("the sound the item should play when moved (optional)")]
    public AudioClip MovedSound;
    /// the sound the item should play when dropped (optional)
    [Tooltip("the sound the item should play when dropped (optional)")]
    public AudioClip DroppedSound;
    /// if this is set to false, default sounds won't be used and no sound will be played
    [Tooltip("if this is set to false, default sounds won't be used and no sound will be played")]
    public bool UseDefaultSoundsIfNull = true;

}
