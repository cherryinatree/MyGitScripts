using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cherry.Inventory;
using Unity.VisualScripting;

public class CraftingGridSlotView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Button button;

    public Action onClick;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }
    }

    public void Set(ItemDefinition item, int amount)
    {
        if (icon != null)
        {
            icon.enabled = item != null;
            // NOTE: rename "icon" below if your ItemDefinition uses a different field/property
            icon.sprite = item != null ? item.Icon : null;
        }

        if (amountText != null)
            amountText.text = (item != null && amount > 0) ? amount.ToString() : "";
    }

    public void SetEmpty() => Set(null, 0);

    public void SetInteractable(bool canInteract)
    {
        if (button != null) button.interactable = canInteract;
    }
}
