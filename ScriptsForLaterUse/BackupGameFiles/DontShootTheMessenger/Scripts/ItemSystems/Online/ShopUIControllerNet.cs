// ShopUIControllerNet.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUIControllerNet : MonoBehaviour
{
    [Header("List")]
    public Transform listContainer;   // VerticalLayoutGroup
    public GameObject rowPrefab;      // prefab with ShopItemRow

    [Header("Header")]
    public TMP_Text titleText;
    public TMP_Text creditsText;
    public Button closeButton;

    ShopTriggerNet _trigger;
    ShopVendorNet _vendor;
    CharacterInventoryNet _localInv;
    ItemDatabase _db;

    public void Bind(ShopTriggerNet trigger, ShopVendorNet vendor, CharacterInventoryNet localInv, ItemDatabase db)
    {
        _trigger = trigger;
        _vendor = vendor;
        _localInv = localInv;
        _db = db;

        if (_db) _db.Init();

        if (titleText) titleText.text = "Shop";
        if (closeButton)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        BuildList();
        RefreshCredits();
        // Update credits live if you want:
        _localInv.credits.OnValueChanged += (_, __) => RefreshCredits();
    }

    void OnDisable()
    {
        if (_localInv != null)
            _localInv.credits.OnValueChanged -= (_, __) => RefreshCredits(); // if you want robust removal, cache the delegate
    }

    void RefreshCredits()
    {
        if (creditsText && _localInv)
            creditsText.text = $"Credits: {_localInv.credits.Value}";
    }

    public void BuildList()
    {
        if (!listContainer || !rowPrefab || !_vendor) return;
        foreach (Transform c in listContainer) Destroy(c.gameObject);

        foreach (var e in _vendor.stock)
        {
            var def = _db ? _db.Get(e.itemId) : null;
            string display = def ? def.displayName : e.itemId;
            Sprite icon = def ? def.icon : null;
            bool available = e.quantity != 0; // -1 = infinite

            var rowGo = Instantiate(rowPrefab, listContainer);
            var row = rowGo.GetComponent<ShopItemRow>() ?? rowGo.AddComponent<ShopItemRow>();
            row.Setup(this, e.itemId, display, icon, e.priceEach >= 0 ? e.priceEach : (def ? def.baseBuyPrice : 0), available);
        }
    }

    public void BuyOne(string itemId)
    {
        _trigger.UI_Buy(itemId, 1);
        // No local changes; server will spawn a pickup. Credits UI updates via NetworkVariable.
    }
}
