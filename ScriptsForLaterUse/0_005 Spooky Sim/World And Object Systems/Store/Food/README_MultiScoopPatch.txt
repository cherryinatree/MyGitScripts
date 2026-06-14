Multi-Scoop Ice Cream Patch
===========================

This patch changes ice cream cones from one flavor to up to 3 scoops.

Replace these scripts in your project:
- FoodTypes.cs
- FoodItem.cs
- FoodOrder.cs
- IngredientDispenser.cs
- FoodCustomerOrder.cs
- FoodItemStateVisuals.cs

Important setup:
1. On the cone prefab, set FoodItem:
   - foodKind = IceCreamCone
   - maxIceCreamScoops = 3
   - iceCreamScoops list empty by default

2. On the cone prefab, add/use FoodItemStateVisuals:
   - Add 3 entries to Scoop Slots, in order: bottom, middle, top.
   - Assign each scoop model GameObject.
   - Assign each scoop Renderer, or leave blank and the script will find it.
   - Assign Chocolate, Vanilla, Strawberry materials.
   - Keep all scoop model objects inactive by default.

3. On ice cream bins, set IngredientDispenser:
   - Action Type = AddIceCreamScoop
   - Ice Cream Flavor = Chocolate / Vanilla / Strawberry
   - Stock Cost Per Use = 0 while testing, or 1 when using stock.

4. Customer matching ignores the order of scoops.
   Example: customer wants 2 vanilla + 1 chocolate.
   These both pass:
   - Vanilla, Vanilla, Chocolate
   - Chocolate, Vanilla, Vanilla

5. Tubs and mixtures still use FoodItem.iceCreamFlavor as a single flavor.
   Only cones use FoodItem.iceCreamScoops.
