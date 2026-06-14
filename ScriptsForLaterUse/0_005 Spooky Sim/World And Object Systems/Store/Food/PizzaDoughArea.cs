using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A refill point for pizza dough.
/// Player brings PizzaDoughBall here, the area consumes it, and the station gains dough servings.
/// </summary>
public class PizzaDoughArea : MonoBehaviour
{
    [Header("Inventory")]
    public FoodStationInventory stationInventory;
    public FoodStockType doughStockType = FoodStockType.PizzaDough;
    public int servingsAddedPerDoughBall = 6;
    public FoodKind acceptedFoodKind = FoodKind.PizzaDoughBall;

    [Header("UI")]
    public TextMeshProUGUI stockText;
    public string stockFormat = "Dough: {0}";

    [Header("Events")]
    public UnityEvent onDoughLoaded;
    public UnityEvent onWrongItem;

    private void Start()
    {
        RefreshText();
    }

    public bool TryLoadDough(PlayerFoodHands hands)
    {
        if (hands == null) return false;
        if (stationInventory == null) return false;
        if (!hands.TryGetHeldFood(out FoodItem item)) return false;

        if (item.foodKind != acceptedFoodKind)
        {
            onWrongItem?.Invoke();
            return false;
        }

        stationInventory.Add(doughStockType, servingsAddedPerDoughBall);
        hands.ConsumeHeldFood();
        RefreshText();
        onDoughLoaded?.Invoke();
        return true;
    }

    public void RefreshText()
    {
        if (stockText == null || stationInventory == null) return;
        stockText.text = string.Format(stockFormat, stationInventory.GetAmount(doughStockType));
    }
}
