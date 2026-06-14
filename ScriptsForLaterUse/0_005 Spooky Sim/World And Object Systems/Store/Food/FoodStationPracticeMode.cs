using System;
using UnityEngine;

/// <summary>
/// Put this on a station parent while testing.
/// It turns the station into free-build / practice mode so you can make food without customers.
/// Remove it or disable it when you want the real customer loop back.
/// </summary>
public class FoodStationPracticeMode : MonoBehaviour
{
    [Header("Customer Requirements")]
    public bool disableCustomerRequirements = true;

    [Header("Stock / Costs")]
    public bool makeStationStockInfinite = true;
    public bool makeCreatedItemsFree = true;
    public bool makeDispensersFree = true;

    [Header("Trash")]
    public bool makeTrashAcceptEverything = true;

    [Header("Pizza Oven")]
    [Tooltip("Leave false if you still want to test the normal oven timing. Turn on if you want to pick pizza up before it is cooked.")]
    public bool allowPickingUpUncookedPizzaFromOven = false;

    [Header("Debug")]
    public bool applyOnStart = true;
    public bool logChanges = true;

    private void Start()
    {
        if (applyOnStart)
            ApplyPracticeMode();
    }

    [ContextMenu("Apply Practice Mode Now")]
    public void ApplyPracticeMode()
    {
        if (disableCustomerRequirements)
            DisableCustomerRequirements();

        if (makeStationStockInfinite)
            MakeStockInfinite();

        if (makeCreatedItemsFree)
            MakeCreatedItemsFree();

        if (makeDispensersFree)
            MakeDispensersFree();

        if (makeTrashAcceptEverything)
            MakeTrashAcceptEverything();

        ApplyPizzaOvenSettings();

        if (logChanges)
            Debug.Log($"{name}: Food station practice mode applied.", this);
    }

    private void DisableCustomerRequirements()
    {
        FoodItemCreator[] foodCreators = GetComponentsInChildren<FoodItemCreator>(true);
        foreach (FoodItemCreator creator in foodCreators)
        {
            creator.assignCreatedFoodToCustomer = false;
            creator.currentCustomer = null;
        }

        PizzaBaseCreator[] pizzaCreators = GetComponentsInChildren<PizzaBaseCreator>(true);
        foreach (PizzaBaseCreator creator in pizzaCreators)
        {
            creator.requireCurrentCustomer = false;
            creator.assignPizzaToCurrentCustomer = false;
            creator.currentCustomer = null;
            creator.lineController = null;
        }
    }

    private void MakeStockInfinite()
    {
        FoodStationInventory[] inventories = GetComponentsInChildren<FoodStationInventory>(true);
        foreach (FoodStationInventory inventory in inventories)
        {
            foreach (FoodStockType stockType in Enum.GetValues(typeof(FoodStockType)))
            {
                inventory.Add(stockType, 9999);
            }

            foreach (FoodStockEntry entry in inventory.stock)
            {
                entry.infinite = true;
                entry.capacity = Mathf.Max(entry.capacity, 9999);
                entry.amount = Mathf.Max(entry.amount, 9999);
            }
        }
    }

    private void MakeCreatedItemsFree()
    {
        FoodItemCreator[] foodCreators = GetComponentsInChildren<FoodItemCreator>(true);
        foreach (FoodItemCreator creator in foodCreators)
            creator.stockCost = 0;

        PizzaBaseCreator[] pizzaCreators = GetComponentsInChildren<PizzaBaseCreator>(true);
        foreach (PizzaBaseCreator creator in pizzaCreators)
            creator.doughCostPerPizza = 0;
    }

    private void MakeDispensersFree()
    {
        IngredientDispenser[] dispensers = GetComponentsInChildren<IngredientDispenser>(true);
        foreach (IngredientDispenser dispenser in dispensers)
            dispenser.stockCostPerUse = 0;
    }

    private void MakeTrashAcceptEverything()
    {
        FoodTrashCan[] trashCans = GetComponentsInChildren<FoodTrashCan>(true);
        foreach (FoodTrashCan trashCan in trashCans)
        {
            trashCan.onlyAcceptRuinedFood = false;
            trashCan.allowIceCream = true;
            trashCan.allowPizza = true;
            trashCan.allowBatches = true;
        }

        FoodTrashTrigger[] trashTriggers = GetComponentsInChildren<FoodTrashTrigger>(true);
        foreach (FoodTrashTrigger trashTrigger in trashTriggers)
        {
            trashTrigger.onlyAcceptRuinedFood = false;
            trashTrigger.allowIceCream = true;
            trashTrigger.allowPizza = true;
            trashTrigger.allowBatches = true;
        }
    }

    private void ApplyPizzaOvenSettings()
    {
        PizzaConveyorOven[] ovens = GetComponentsInChildren<PizzaConveyorOven>(true);
        foreach (PizzaConveyorOven oven in ovens)
            oven.cookedPizzaOnlyCanBePickedUp = !allowPickingUpUncookedPizzaFromOven;
    }
}
