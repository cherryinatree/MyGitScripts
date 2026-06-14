using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Simple ingredient button/funnel for FoodRecipeBatchMaker.
/// Useful for adding Milk/Sugar/Chocolate Base/etc. without creating physical ingredient items yet.
/// </summary>
public class FoodRecipeIngredientButton : MonoBehaviour
{
    [Header("Recipe Target")]
    public FoodRecipeBatchMaker batchMaker;
    public string ingredientId = "Milk";
    public int amountPerUse = 1;

    [Header("Optional Stock Cost")]
    public FoodStationInventory stockInventory;
    public FoodStockType stockType = FoodStockType.GenericToppings;
    public int stockCostPerUse = 0;

    [Header("UI")]
    public TextMeshProUGUI label;
    public string labelFormat = "+{0} {1}";

    [Header("Events")]
    public UnityEvent onIngredientAdded;
    public UnityEvent onIngredientRejected;

    private void Start()
    {
        RefreshLabel();
    }

    public bool TryAddIngredient()
    {
        if (batchMaker == null)
        {
            onIngredientRejected?.Invoke();
            return false;
        }

        if (stockInventory != null && stockCostPerUse > 0)
        {
            if (!stockInventory.TryConsume(stockType, stockCostPerUse))
            {
                onIngredientRejected?.Invoke();
                return false;
            }
        }

        bool added = batchMaker.AddIngredient(ingredientId, amountPerUse);

        if (added)
            onIngredientAdded?.Invoke();
        else
            onIngredientRejected?.Invoke();

        return added;
    }

    public void RefreshLabel()
    {
        if (label == null) return;
        label.text = string.Format(labelFormat, amountPerUse, ingredientId);
    }
}
