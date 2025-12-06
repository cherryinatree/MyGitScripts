using TMPro;
using UnityEngine;

public class CheckoutStore : MonoBehaviour
{
    public Transform contentParent;
    public TextMeshProUGUI totalPriceText;
    public TextMeshProUGUI errorText;
    public TextMeshProUGUI money;
    public GameObject checkoutItemPanelPrefab;
    private ShopService shopService;

    private void OnEnable()
    {
        shopService = FindFirstObjectByType<ShopService>();
    }
    
    public void UpdateMoney()
    {
        money.text = $"Space Bucks: ${SaveData.Current.mainData.playerData.money}";
    }

    public void UpdateTotalPrice()
    {
        totalPriceText.text = $"Total: ${shopService.CartTotal()}";
    }
    public void AddCheckOutItem(ShopItemDefinition itemDefinition, int howMany)
    {
        Instantiate(checkoutItemPanelPrefab, contentParent).GetComponent<ChekoutItemPanel>().Setup(itemDefinition,howMany, this);
    }

    public void Buy()
    {

        var shopService = FindFirstObjectByType<ShopService>();
        if (shopService.CanCheckout(out string reason))
        {

            shopService.Checkout();
            errorText.text = "Enjoy Your New Stuff!!!";

            for (int i = 0; i < contentParent.childCount; i++)
            {
                Destroy(contentParent.GetChild(i).gameObject);
            }
            UpdateTotalPrice();
        }
        else
        {
            errorText.text = reason;
        }
    }
}
