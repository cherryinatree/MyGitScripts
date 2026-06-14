FoodStation Enum Fix Patch
==========================

Replace these files in your Unity project:

- FoodTypes.cs
- IngredientDispenser.cs

Why this patch exists:
Unity's Inspector does not behave well when two enum names share the exact same number. The previous multi-scoop patch made SetIceCreamFlavor and AddIceCreamScoop both equal 0 so older scenes would still work, but Unity could show the same stored value as SetIceCreamFlavor even after you picked AddIceCreamScoop.

This patch gives AddIceCreamScoop its own value so it stays selected in the Inspector. Existing dispensers are preserved:

- SetIceCreamFlavor = 0
- AddTopping = 1
- AddSauce = 2
- AddCheese = 3
- AddIceCreamScoop = 4

In multi-scoop mode, both SetIceCreamFlavor and AddIceCreamScoop call FoodItem.AddIceCreamScoop(). So old ice cream bins still work, and new bins can use AddIceCreamScoop without changing back.

Recommended ice cream bin setup:

- Action Type = AddIceCreamScoop
- Ice Cream Flavor = Chocolate, Vanilla, or Strawberry
- Stock Cost Per Use = 0 while testing
