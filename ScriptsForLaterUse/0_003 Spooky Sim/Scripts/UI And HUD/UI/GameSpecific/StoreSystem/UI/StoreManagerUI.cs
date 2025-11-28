using System.Linq;
using UnityEngine;

public class StoreManagerUI : MonoBehaviour
{

    public Transform content;
    public GameObject cartWindow;
    public CheckoutStore checkoutStore;

    public GameObject panelPrefab;
    public ShopCatalog catalog;
    public ShopService shopService;
    public int itemsPerRow = 3;

    private ShopItemDefinition[] displayedItems;

    private void OnEnable()
    {
        //int playerLevel = SaveData.Current.mainData.playerData.level;
        // For testing purposes, set player level to 0
        int playerLevel = 0;
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        displayedItems = new ShopItemDefinition[catalog.items.Count];
        for(int i = 0; i < catalog.items.Count; i++)
        {
            displayedItems[i] = catalog.items[i];
        }
        OrganizeDisplayedItemsByLevelRequired();

        for (int i = 0; i < displayedItems.Length; i += itemsPerRow)
        {
            ShopItemDefinition item0 = catalog.items[i];
            int itemLevelRequired = item0.levelRequired;
            ShopItemDefinition item1 = (i + 1 < catalog.items.Count) ? catalog.items[i + 1] : null;
            if (item1 != null)
            {
                if (item1.levelRequired != itemLevelRequired)
                {
                    item1 = null;
                    ShopItemDefinition item2StandIn = null;
                    i -= 2;
                    Instantiate(panelPrefab, content).GetComponent<OnlineStorePanel>().Setup(item0, item1, item2StandIn, playerLevel);
                    continue;
                }
            }
            ShopItemDefinition item2 = (i + 2 < catalog.items.Count) ? catalog.items[i + 2] : null;
            if (item2 != null)
            {
                if (item2.levelRequired != itemLevelRequired)
                {
                    item2 = null;
                    i -= 1;
                    Instantiate(panelPrefab, content).GetComponent<OnlineStorePanel>().Setup(item0, item1, item2, playerLevel);
                    continue;
                }
            }
            Instantiate(panelPrefab, content).GetComponent<OnlineStorePanel>().Setup(item0, item1, item2, playerLevel);

        }
        
    }

    private void OrganizeDisplayedItemsByLevelRequired()
    {
           displayedItems = displayedItems.OrderBy(item => item.levelRequired).ToArray();
    }

    public void OpenCart()
    {
        cartWindow.SetActive(true);
        checkoutStore.UpdateMoney();
        checkoutStore.UpdateTotalPrice();
        foreach (Transform child in checkoutStore.contentParent)
        {
            Destroy(child.gameObject);
        }
        var cartItems = shopService.GetCartItems();
        foreach (var item in cartItems)
        {
            checkoutStore.AddCheckOutItem(item.item, item.quantity);
        }
    }
}
