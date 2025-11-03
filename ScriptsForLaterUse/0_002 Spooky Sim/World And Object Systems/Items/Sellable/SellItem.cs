using UnityEngine;

public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }

/// <summary>
/// Data-only definition for an item that can be bought/sold.
/// Use this as the single source of truth for static item data.
/// </summary>
[CreateAssetMenu(menuName = "Game/Economy/Sellable Item", fileName = "New Sellable Item")]
public class SellableItem : ScriptableObject
{
    [Header("Identity")]
    [SerializeField, Tooltip("Stable unique id. Auto-generated if left empty.")]
    private int itemId;

    [SerializeField] private string displayName = "New Item";
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;

    [Header("Pricing")]
    [SerializeField, Min(0), Tooltip("Nominal base price used for buy/sell calculations.")]
    private float price = 1;

    // -------- Public API (read-only properties) --------
    public int ItemId => itemId;
    public float Price => price;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
}
