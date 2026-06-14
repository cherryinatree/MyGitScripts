// ItemUseRouter.cs
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class ItemUseRouter : MonoBehaviour
{
    public HotbarUIController hotbar;
    public CharacterInventoryNet inv;
    public EquipmentNet equip;
    public PlacementSystem placement;
    public ItemDatabase db;

    void Awake()
    {
        if (!inv) inv = FindAnyObjectByType<CharacterInventoryNet>();
        if (!equip && inv) equip = inv.GetComponent<EquipmentNet>();
        if (!placement && inv) placement = inv.GetComponent<PlacementSystem>();
        if (!db && inv) db = inv.db;
        if (db) db.Init();
    }

    void Update()
    {
        // Example binding: Left click to use active item (change if you have your own input system)
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            UseActive();
        }
    }

    public void UseActive()
    {
        if (!hotbar) return;
        var id = hotbar.GetActiveItemId();
        if (string.IsNullOrEmpty(id)) return;

        var def = db.Get(id);
        if (!def) return;

        // Placeable: begin client ghost placement
        if (def.isPlaceable && def.placeablePrefab)
        {
            placement.BeginPlace(def);
            return;
        }

        // Equip weapon/armor
        if (def.equipSlot != EquipSlot.None)
        {
            equip.RequestEquipServerRpc(inv.NetworkObject, id, def.equipSlot);
            // If equipping should consume the inventory stack, uncomment in EquipmentNet and refresh here.
            hotbar.Repaint();
            return;
        }

        // Simple consumable behavior for supplies (customize to your game)
        if (def.category == ItemCategory.Supply)
        {
            // TODO: apply effect (heal, buff, etc.) on server.
            // For now just remove one:
            inv.RequestRemoveItemServerRpc(id, 1);
            hotbar.Repaint();
            return;
        }

        // Ammo/Cosmetic/Decoration (non-placeable) default: no direct use
        Debug.Log($"No direct use configured for {def.displayName} [{def.category}].");
    }
}
