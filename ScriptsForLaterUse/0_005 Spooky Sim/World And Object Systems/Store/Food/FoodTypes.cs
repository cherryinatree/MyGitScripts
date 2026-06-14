using System;

public enum CustomerOrderType
{
    IceCream,
    Pizza
}

public enum FoodKind
{
    None,
    IceCreamCone,
    IceCreamMixture,
    IceCreamTub,
    PizzaDoughBall,
    RawPizza,
    CookedPizza
}

public enum IceCreamFlavor
{
    None,
    Chocolate,
    Vanilla,
    Strawberry
}

public enum AmountLevel
{
    None,
    Light,
    Normal,
    Heavy
}

public enum FoodIngredient
{
    None,

    // Ice cream toppings
    Sprinkles,
    CookieCrumbs,
    ChocolateSyrup,
    CaramelSyrup,
    CandyPieces,

    // Pizza toppings
    Pepperoni,
    Peppers,
    Olives,
    Onions,
    Pineapple
}

public enum FoodStockType
{
    ChocolateIceCream,
    VanillaIceCream,
    StrawberryIceCream,
    PizzaDough,
    GenericToppings
}

public enum DispenserActionType
{
    // Keep the original values so existing scene objects do not get remapped.
    // In multi-scoop mode, this old option behaves the same as AddIceCreamScoop.
    SetIceCreamFlavor = 0,

    AddTopping = 1,
    AddSauce = 2,
    AddCheese = 3,

    // Separate value so Unity's Inspector will not collapse it back to SetIceCreamFlavor.
    AddIceCreamScoop = 4
}

public static class FoodAmountUtility
{
    public static AmountLevel Increase(AmountLevel current)
    {
        return current switch
        {
            AmountLevel.None => AmountLevel.Light,
            AmountLevel.Light => AmountLevel.Normal,
            AmountLevel.Normal => AmountLevel.Heavy,
            _ => AmountLevel.Heavy
        };
    }

    public static string ToFriendlyText(this AmountLevel amount)
    {
        return amount switch
        {
            AmountLevel.None => "no",
            AmountLevel.Light => "light",
            AmountLevel.Normal => "normal",
            AmountLevel.Heavy => "heavy",
            _ => amount.ToString()
        };
    }
}
