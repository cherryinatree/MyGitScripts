// ShopTrigger.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ShopTrigger : MonoBehaviour
{
    public ShopVendor vendor;
    public GameObject shopUIPanel; // assign a canvas panel
    CharacterInventory current;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        current = other.GetComponentInParent<CharacterInventory>();
        if (current && shopUIPanel) shopUIPanel.SetActive(true);
        // Populate UI list here from vendor.stock...
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<CharacterInventory>() == current)
        {
            if (shopUIPanel) shopUIPanel.SetActive(false);
            current = null;
        }
    }

    // Called by UI Buy button
    public void UI_Buy(ItemDefinition item, int qty)
    {
        if (current) vendor.TryBuy(current, item, qty);
    }

    public void UI_Sell(ItemDefinition item, int qty)
    {
        if (current) vendor.TrySell(current, item, qty);
    }
}
