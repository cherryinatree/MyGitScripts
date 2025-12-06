using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChekoutItemPanel : MonoBehaviour
{
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemPriceText;
    public TextMeshProUGUI howManyText;
    public int quantity = 1;
    public Button addItem;
    public Button removeItem;
    private ShopService shopService;
    private ShopItemDefinition currentItemDefinition;
    private CheckoutStore checkoutStore;


    public void Setup(ShopItemDefinition itemDefinition, int howManyItems, CheckoutStore checkout)
    {
        checkoutStore = checkout;
        shopService = FindFirstObjectByType<ShopService>();
        quantity = howManyItems;
        howManyText.text = quantity.ToString();
        itemNameText.text = itemDefinition.displayName;
        itemPriceText.text = $"${itemDefinition.price}";
        currentItemDefinition = itemDefinition;
    }

    public void AddItem()
    {

        shopService.AddToCart(currentItemDefinition);
        quantity++;
        howManyText.text = quantity.ToString();
        checkoutStore.UpdateTotalPrice();
    }

    public void RemoveItem()
    {
        if (quantity > 1)
        {
            shopService.RemoveFromCart(currentItemDefinition, 1);
            quantity--;
            howManyText.text = quantity.ToString();
            checkoutStore.UpdateTotalPrice();

        }
        if (quantity == 1)
        {
            shopService.RemoveFromCart(currentItemDefinition);
            checkoutStore.UpdateTotalPrice();
            Destroy(gameObject);
        }
    }
}
