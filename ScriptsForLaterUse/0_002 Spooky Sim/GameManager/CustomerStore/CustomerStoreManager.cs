using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class CustomerStoreManager : MonoBehaviour
{
    public MerchandisingFixtures[] fixtures;
    public CheckOutLine[] checkOutLines;


    public void SubscribeCheckoutLines(CheckOutLine checkOut)
    {
        if(checkOutLines == null)
        {
            checkOutLines = new CheckOutLine[0];
        }
        System.Array.Resize(ref checkOutLines, checkOutLines.Length + 1);
        checkOutLines[checkOutLines.Length - 1] = checkOut;
    }

    public void SubscribeFixture(MerchandisingFixtures fixture)
    {
       if(fixtures == null)
        {
            fixtures = new MerchandisingFixtures[0];
        }

        System.Array.Resize(ref fixtures, fixtures.Length + 1);
        fixtures[fixtures.Length - 1] = fixture;
    }
    
    public CheckOutLine GetRandomCheckoutLine()
    {
        if(checkOutLines == null || checkOutLines.Length == 0)
        {
            return null; // Return null as fallback
        }


        int randomIndex = Random.Range(0, checkOutLines.Length-1);
        return checkOutLines[randomIndex];
    }

    public Transform GetItemPosition(Carryable item)
    {
        return item.GetFixtureParent().viewingArea.transform;
    }

    public Carryable GetRandomItem()
    {
        if (fixtures == null || fixtures.Length == 0)
        {
            return null; // Return null as fallback
        }

        // Select a random fixture
        int randomIndex = Random.Range(0, fixtures.Length);
        MerchandisingFixtures selectedFixture = fixtures[randomIndex];


        List<Carryable> availableItems = new List<Carryable>();
        // Iterate through shelves to find a random item
        foreach (Shelf shelf in selectedFixture.shelves)
        {
            foreach (ShelfSlot slot in shelf.Slots)
            {
                if (slot.isOccupied())
                {
                    availableItems.Add(slot.GetItem());
                }
            }
        }

        if(availableItems.Count > 0)
        {
            return availableItems[Random.Range(0, availableItems.Count-1)];
        }

        return null; // Return null if no items found
    }
}
