// ShopItemRow.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemRow : MonoBehaviour
{
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text priceText;
    public Button buyButton;

    string _itemId;
    ShopUIControllerNet _ui;

    public void Setup(ShopUIControllerNet ui, string itemId, string displayName, Sprite itemIcon, int priceEach, bool available)
    {
        _ui = ui;
        _itemId = itemId;

        if (icon) icon.sprite = itemIcon;
        if (nameText) nameText.text = displayName;
        if (priceText) priceText.text = $"${priceEach}";
        if (buyButton)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => _ui.BuyOne(_itemId));
            buyButton.interactable = available;
        }
    }
}
