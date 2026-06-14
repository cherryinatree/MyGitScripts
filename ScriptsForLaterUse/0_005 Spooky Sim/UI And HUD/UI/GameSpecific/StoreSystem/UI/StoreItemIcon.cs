using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreItemIcon : MonoBehaviour
{
    public Image icon;
    public GameObject addToCart;
    public GameObject removeCart;
    public TextMeshProUGUI howManyInCartText;
    public TextMeshProUGUI priceText;
    private int howManyInCart =0;

    private ShopItemDefinition itemDefinition;
    private ShopService shopService;
   
    public void Setup(ShopItemDefinition itemDefinition)
    {
        shopService = FindFirstObjectByType<ShopService>();
        this.itemDefinition = itemDefinition;
        icon.sprite = itemDefinition.icon;
        priceText.text = $"${itemDefinition.price:F2}";
        //addToCart.onClick.AddListener(AddToCart);
        //removeCart.onClick.AddListener(RemoveFromCart);
        RemoveActive();
    }

    private void RemoveActive()
    {
        removeCart.gameObject.SetActive(howManyInCart > 0);
    }

    public void AddToCart()
    {
        shopService.AddToCart(itemDefinition);
        RemoveActive();
        howManyInCart++;
        howManyInCartText.text = howManyInCart.ToString();
    }

    public void RemoveFromCart()
    {
        Debug.Log("Remove from cart clicked");
        if (howManyInCart > 0)
        {
            shopService.RemoveFromCart(itemDefinition);
            howManyInCart--;
            howManyInCartText.text = howManyInCart.ToString();
        }
        RemoveActive();
    }
}
