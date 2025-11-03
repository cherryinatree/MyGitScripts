using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class Shelf : MonoBehaviour
{
    [Tooltip("Assign empty transforms that represent snap points on the shelf.")]
    public List<ShelfSlot> Slots;
    private int itemId = -1;
    private MerchandisingFixtures parentFixture;

    private readonly Dictionary<Transform, Carryable> _occupancy = new();

    private void Awake()
    {
        if (Slots == null)
            Slots = new List<ShelfSlot>();
    }


    public void SetMyParentFixture(MerchandisingFixtures fixture)
    {
        parentFixture = fixture;
        foreach (ShelfSlot slot in Slots)
        {
            slot.SetMyParentFixture(fixture);
        }
    }

    public bool TryGetFreeSlot(out ShelfSlot slot)
    {

        foreach (var kv in Slots)
        {
            if (!kv.isOccupied())
            {
                slot = kv;
                return true;
            }
        }
        slot = null;
        return false;
    }

    public void DockIntoSlot(Carryable item, ShelfSlot slot)
    {

        if(!Slots.Contains(slot)) return;
        if (slot.isOccupied()) return;
        if (item.gameObject.GetComponent<SellItemHolder>().ItemId == itemId || itemId == -1)
        {
            item.DockTo(slot, asChild: true);
            if(itemId == -1) itemId = item.gameObject.GetComponent<SellItemHolder>().ItemId;
        }
        else
        {
            return;
        }
    }

    public Carryable UndockFromSlot()
    {
        for (int i = Slots.Count-1; i >=0 ; i--)
        {
            var slot = Slots[i];
            if (slot.isOccupied())
            {
                Carryable item = slot.GetItem();
                item.UndockToWorld();
                slot.ClearSlot();
                return item;
            }
        }

        return null;
    }


    public void ClearSlot(ShelfSlot slot)
    {
        if (!Slots.Contains(slot)) return;
        slot.ClearSlot();

        foreach (var kv in Slots)
        {
            if (kv.isOccupied())
                return;
        }
        itemId = -1;
    }

    public bool IsFull()
    {
        foreach (var kv in Slots)
            if (!kv.isOccupied()) return false;
        return true;
    }
}
