using UnityEngine;

public class IngredientDispenser : MonoBehaviour
{
    [Header("Dispenser")]
    public DispenserActionType actionType;
    public IceCreamFlavor iceCreamFlavor = IceCreamFlavor.None;
    public FoodIngredient topping = FoodIngredient.None;

    [Header("Stock")]
    public FoodStationInventory inventory;
    public FoodStockType stockType;
    public int stockCostPerUse = 0;

    [Header("Limits")]
    public int maxDuplicateToppings = 1;

    [Header("Debug")]
    [Tooltip("Turn this on while setting up the station. It prints exactly why the dispenser did or did not work.")]
    public bool verboseDebug = true;

    public bool TryUse(PlayerFoodHands hands)
    {
        if (hands == null)
            return Fail("No PlayerFoodHands was provided. Assign Hands on InteractRelay or make sure the player has PlayerFoodHands.");

        if (!hands.TryGetHeldFood(out FoodItem foodItem))
            return Fail($"PlayerFoodHands exists on '{hands.name}', but it is not holding a FoodItem.");

        if (foodItem == null)
            return Fail("Held food reference was null.");

        if (foodItem.isRuined)
            return Fail($"Held food '{foodItem.name}' is marked ruined, so it cannot receive ingredients.");

        if (!CanApply(foodItem, out string reason))
            return Fail(reason);

        if (inventory != null && stockCostPerUse > 0)
        {
            if (!inventory.TryConsume(stockType, stockCostPerUse))
                return Fail($"Out of stock. Needed {stockCostPerUse} of {stockType} from inventory '{inventory.name}'.");
        }

        bool applied = Apply(foodItem, out string applyReason);
        if (!applied)
            return Fail(applyReason);

        FoodItemStateVisuals visuals = foodItem.GetComponentInChildren<FoodItemStateVisuals>();
        if (visuals != null)
            visuals.Refresh();

        if (verboseDebug)
            Debug.Log($"{name}: Applied {actionType} to {foodItem.name}. Now: {foodItem.DebugDescription()}", this);

        return true;
    }

    private bool CanApply(FoodItem foodItem, out string reason)
    {
        reason = "";

        switch (actionType)
        {
            case DispenserActionType.SetIceCreamFlavor:
            case DispenserActionType.AddIceCreamScoop:
                if (foodItem.foodKind != FoodKind.IceCreamCone)
                {
                    reason = $"Held food is '{foodItem.foodKind}', not IceCreamCone. The scoop bin only works on a cone.";
                    return false;
                }

                if (iceCreamFlavor == IceCreamFlavor.None)
                {
                    reason = "Ice Cream Flavor is None. Set this bin to Chocolate, Vanilla, or Strawberry.";
                    return false;
                }

                if (!foodItem.CanReceiveIceCreamScoop())
                {
                    int current = foodItem.GetTotalIceCreamScoops();
                    reason = $"Cone cannot receive another scoop. Current scoops: {current}. Max scoops: {foodItem.maxIceCreamScoops}. Make sure the FoodItem Ice Cream Scoops list starts empty, size 0.";
                    return false;
                }

                return true;

            case DispenserActionType.AddTopping:
                if (foodItem.foodKind != FoodKind.IceCreamCone && foodItem.foodKind != FoodKind.RawPizza)
                {
                    reason = $"Held food is '{foodItem.foodKind}', but toppings can only be added to IceCreamCone or RawPizza.";
                    return false;
                }

                if (topping == FoodIngredient.None)
                {
                    reason = "Topping is None. Pick a real topping on the dispenser.";
                    return false;
                }

                if (CountTopping(foodItem, topping) >= maxDuplicateToppings)
                {
                    reason = $"This food already has the max allowed copies of {topping}.";
                    return false;
                }

                return true;

            case DispenserActionType.AddSauce:
                if (!foodItem.CanReceivePizzaIngredients())
                {
                    reason = $"Held food is '{foodItem.foodKind}', not RawPizza. Sauce only works on raw pizza.";
                    return false;
                }

                if (foodItem.sauceAmount == AmountLevel.Heavy)
                {
                    reason = "Sauce is already Heavy.";
                    return false;
                }

                return true;

            case DispenserActionType.AddCheese:
                if (!foodItem.CanReceivePizzaIngredients())
                {
                    reason = $"Held food is '{foodItem.foodKind}', not RawPizza. Cheese only works on raw pizza.";
                    return false;
                }

                if (foodItem.cheeseAmount == AmountLevel.Heavy)
                {
                    reason = "Cheese is already Heavy.";
                    return false;
                }

                return true;

            default:
                reason = $"Unsupported dispenser action: {actionType}";
                return false;
        }
    }

    private bool Apply(FoodItem foodItem, out string reason)
    {
        reason = "";

        switch (actionType)
        {
            case DispenserActionType.SetIceCreamFlavor:
            case DispenserActionType.AddIceCreamScoop:
                if (!foodItem.AddIceCreamScoop(iceCreamFlavor))
                {
                    reason = $"FoodItem refused AddIceCreamScoop({iceCreamFlavor}). Check Food Kind, max scoops, and flavor.";
                    return false;
                }
                return true;

            case DispenserActionType.AddTopping:
                foodItem.AddTopping(topping);
                return true;

            case DispenserActionType.AddSauce:
                foodItem.AddSauceScoop();
                return true;

            case DispenserActionType.AddCheese:
                foodItem.AddCheeseScoop();
                return true;
        }

        reason = $"No apply branch for action {actionType}.";
        return false;
    }

    private bool Fail(string reason)
    {
        if (verboseDebug)
            Debug.LogWarning($"{name}: IngredientDispenser failed. {reason}", this);

        return false;
    }

    private int CountTopping(FoodItem foodItem, FoodIngredient ingredient)
    {
        if (foodItem.toppings == null) return 0;

        int count = 0;
        foreach (FoodIngredient toppingEntry in foodItem.toppings)
        {
            if (toppingEntry == ingredient)
                count++;
        }

        return count;
    }
}
