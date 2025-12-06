using UnityEngine;

public class MerchandisingFixtures : MonoBehaviour
{
    public Shelf[] shelves;
    public GameObject viewingArea;

    public void Awake()
    {
        CustomerStoreManager storeManager = FindFirstObjectByType<CustomerStoreManager>();
        if(storeManager != null)
        {
            storeManager.SubscribeFixture(this);
        }

        foreach (Shelf item in shelves)
        {
            item.SetMyParentFixture(this);
        }
    }

    public ShelfSlot FindShelfSlot(GameObject item)
    {
        foreach (Shelf shelf in shelves)
        {
            ShelfSlot slot = shelf.FindShelfSlot(item);
            if (slot != null)
            {
                return slot;
            }
        }
        return null;
    }
}
