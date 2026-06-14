using System.Collections.Generic;
using UnityEngine;

public class FoodItem : MonoBehaviour
{
    [Header("Food State")]
    public FoodKind foodKind = FoodKind.None;
    public int assignedOrderId = -1;
    public FoodCustomerOrder assignedCustomer;

    [Header("Ice Cream")]
    [Tooltip("Used by ice cream mixtures and tubs. Cones now use iceCreamScoops instead.")]
    public IceCreamFlavor iceCreamFlavor = IceCreamFlavor.None;

    [Tooltip("Cones can hold up to this many scoops.")]
    public int maxIceCreamScoops = 3;

    [Tooltip("The flavors currently placed on this cone. Order only affects visuals, not customer matching.")]
    public List<IceCreamFlavor> iceCreamScoops = new List<IceCreamFlavor>();

    [Header("Pizza")]
    public AmountLevel sauceAmount = AmountLevel.None;
    public AmountLevel cheeseAmount = AmountLevel.None;
    public bool isCooked;

    [Header("Shared")]
    public List<FoodIngredient> toppings = new List<FoodIngredient>();
    public bool isRuined;

    public void AssignToCustomer(FoodCustomerOrder customer)
    {
        assignedCustomer = customer;
        assignedOrderId = customer != null ? customer.CurrentOrderId : -1;
    }

    public bool CanReceiveIceCreamFlavor()
    {
        return CanReceiveIceCreamScoop();
    }

    public bool CanReceiveIceCreamScoop()
    {
        if (foodKind != FoodKind.IceCreamCone) return false;
        if (iceCreamScoops == null) iceCreamScoops = new List<IceCreamFlavor>();
        return iceCreamScoops.Count < maxIceCreamScoops;
    }

    public bool CanReceivePizzaIngredients()
    {
        return foodKind == FoodKind.RawPizza;
    }

    public void SetIceCreamFlavor(IceCreamFlavor flavor)
    {
        AddIceCreamScoop(flavor);
    }

    public bool AddIceCreamScoop(IceCreamFlavor flavor)
    {
        if (foodKind != FoodKind.IceCreamCone) return false;
        if (flavor == IceCreamFlavor.None) return false;
        if (!CanReceiveIceCreamScoop()) return false;

        if (iceCreamScoops == null) iceCreamScoops = new List<IceCreamFlavor>();
        iceCreamScoops.Add(flavor);

        // Legacy field: keeps old debug/UI scripts from showing None forever.
        if (iceCreamFlavor == IceCreamFlavor.None)
            iceCreamFlavor = flavor;

        return true;
    }

    public int CountIceCreamScoops(IceCreamFlavor flavor)
    {
        if (iceCreamScoops == null) return 0;

        int count = 0;
        foreach (IceCreamFlavor scoop in iceCreamScoops)
        {
            if (scoop == flavor)
                count++;
        }

        return count;
    }

    public int GetTotalIceCreamScoops()
    {
        if (iceCreamScoops == null) return 0;
        return iceCreamScoops.Count;
    }

    public void AddTopping(FoodIngredient topping)
    {
        if (topping == FoodIngredient.None) return;
        if (toppings == null) toppings = new List<FoodIngredient>();
        toppings.Add(topping);
    }

    public void AddSauceScoop()
    {
        if (!CanReceivePizzaIngredients()) return;
        sauceAmount = FoodAmountUtility.Increase(sauceAmount);
    }

    public void AddCheeseScoop()
    {
        if (!CanReceivePizzaIngredients()) return;
        cheeseAmount = FoodAmountUtility.Increase(cheeseAmount);
    }

    public void MarkCooked()
    {
        if (foodKind == FoodKind.RawPizza)
            foodKind = FoodKind.CookedPizza;

        isCooked = true;
    }

    public void MarkRuined()
    {
        isRuined = true;
    }

    public string DebugDescription()
    {
        if (foodKind == FoodKind.IceCreamCone)
            return $"Ice cream cone, scoops: {GetTotalIceCreamScoops()} " +
                   $"(Chocolate {CountIceCreamScoops(IceCreamFlavor.Chocolate)}, " +
                   $"Vanilla {CountIceCreamScoops(IceCreamFlavor.Vanilla)}, " +
                   $"Strawberry {CountIceCreamScoops(IceCreamFlavor.Strawberry)}), toppings: {toppings.Count}";

        if (foodKind == FoodKind.RawPizza || foodKind == FoodKind.CookedPizza)
            return $"{foodKind}, sauce: {sauceAmount}, cheese: {cheeseAmount}, toppings: {toppings.Count}";

        return foodKind.ToString();
    }
}
