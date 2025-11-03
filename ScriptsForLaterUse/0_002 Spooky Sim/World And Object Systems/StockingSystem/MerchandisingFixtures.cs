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
}
