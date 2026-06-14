// HotbarUIController.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class HotbarUIController : MonoBehaviour
{
    [Header("Wiring")]
    public Transform slotsParent;          // parent containing 8 SlotXX objects
    public ItemDatabase db;                // assign (or leave null if your other scripts auto-load Resources/ItemDatabase)
    public PlacementSystem placement;      // player’s PlacementSystem (for placeables)
    public EquipmentNet equipment;         // player’s EquipmentNet (for equip)
    public CharacterInventoryNet inv;      // player’s CharacterInventoryNet

    [Header("Config")]
    public int hotbarSize = 8;
    public bool autoFillFromInventory = true;

    HotbarSlotUI[] slotUIs;
    string[] slotItemIds;                  // itemId per slot (null/empty if none)
    int activeIndex = 0;

    void Awake()
    {
        // Find local player refs if not assigned
        if (!inv) inv = FindAnyObjectByType<CharacterInventoryNet>();
        if (!equipment && inv) equipment = inv.GetComponent<EquipmentNet>();
        if (!placement && inv) placement = inv.GetComponent<PlacementSystem>();
        if (!db && inv) db = inv.db;

        slotUIs = new HotbarSlotUI[hotbarSize];
        slotItemIds = new string[hotbarSize];

        // Cache slot components
        for (int i = 0; i < hotbarSize; i++)
        {
            var child = slotsParent.GetChild(i);
            var slot = child.GetComponent<HotbarSlotUI>();
            if (!slot) slot = child.gameObject.AddComponent<HotbarSlotUI>();
            slot.Init(i, OnSlotClicked);
            slotUIs[i] = slot;
        }
    }

    void OnEnable()
    {
        if (inv != null)
        {
            inv.contents.OnListChanged += OnInventoryChanged;
        }
        BuildFromInventory();
        SelectIndex(0);
    }

    void OnDisable()
    {
        if (inv != null)
        {
            inv.contents.OnListChanged -= OnInventoryChanged;
        }
    }
    void OnInventoryChanged(NetworkListEvent<NetworkItemStack> e)
    {
        BuildFromInventory();
    }
    void BuildFromInventory()
    {
        if (!autoFillFromInventory || inv == null) { RefreshVisuals(); return; }

        // Distinct by itemId, preserve order as they appear in contents
        var distinctIds = new List<string>();
        foreach (var st in inv.contents)
        {
            var id = st.itemId.ToString();
            if (string.IsNullOrEmpty(id)) continue;
            if (!distinctIds.Contains(id)) distinctIds.Add(id);
        }

        for (int i = 0; i < hotbarSize; i++)
        {
            slotItemIds[i] = i < distinctIds.Count ? distinctIds[i] : null;
        }

        RefreshVisuals();
    }

    void RefreshVisuals()
    {
        if (db) db.Init();

        for (int i = 0; i < hotbarSize; i++)
        {
            string id = slotItemIds[i];
            var slot = slotUIs[i];

            if (string.IsNullOrEmpty(id))
            {
                slot.SetIcon(null);
                slot.SetCount(0);
                continue;
            }

            var def = db ? db.Get(id) : null;
            slot.SetIcon(def ? def.icon : null);

            // Sum stacks by id
            int total = inv != null ? inv.CountLocal(id) : 0;
            slot.SetCount(total);
        }

        // Update selection highlight
        for (int i = 0; i < hotbarSize; i++)
            slotUIs[i].SetSelected(i == activeIndex);
    }

    void OnSlotClicked(int index) => SelectIndex(index);

    public void SelectIndex(int index)
    {
        activeIndex = Mathf.Clamp(index, 0, hotbarSize - 1);
        for (int i = 0; i < hotbarSize; i++)
            slotUIs[i].SetSelected(i == activeIndex);
    }

    public string GetActiveItemId() => slotItemIds[activeIndex];

    // Expose for input script:
    public void Cycle(int delta)
    {
        int idx = activeIndex + delta;
        if (idx < 0) idx = hotbarSize - 1;
        if (idx >= hotbarSize) idx = 0;
        SelectIndex(idx);
    }

    // Call after consuming/equipping to update counts/visuals
    public void Repaint() => RefreshVisuals();
}
