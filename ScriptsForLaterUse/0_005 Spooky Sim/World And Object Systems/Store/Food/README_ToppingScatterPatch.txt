Food Station Topping Scatter Patch
==================================

Files:
- FoodToppingScatterZone.cs
- FoodToppingScatterVisuals.cs

What it does:
- When a FoodItem receives toppings, visual topping prefabs are instantiated at random points.
- Ice cream uses the current top scoop's scatter zone.
- Pizza uses a dedicated topping zone above the pizza, independent of the cheese visual.

Ice Cream Cone Setup:
1. On each scoop model, create/assign a child object that represents the scatter area.
   Example:
   Scoop_Bottom
     ToppingZone_Bottom
   Scoop_Middle
     ToppingZone_Middle
   Scoop_Top
     ToppingZone_Top
2. Add FoodToppingScatterZone to each ToppingZone object.
   Suggested settings for scoops:
   - Surface Mode: UpperHemisphere if the zone transform is at the scoop center
   - OR FlatCircle if the zone transform sits on top of the scoop
3. On the cone prefab, add FoodToppingScatterVisuals.
4. Assign the iceCreamScoopZones array in bottom/middle/top order.
5. In Topping Visuals, add entries for Sprinkles, CookieCrumbs, ChocolateSyrup, etc.
6. Assign prefab models and set Pieces Per Use.

Pizza Setup:
1. On the raw pizza prefab, create an empty child object above the dough called PizzaToppingZone.
2. Add FoodToppingScatterZone to PizzaToppingZone.
   Suggested settings:
   - Surface Mode: FlatCircle or FlatRectangle
   - Radius/Rectangle Size should fit the topping area.
3. On the pizza prefab, add FoodToppingScatterVisuals.
4. Assign pizzaToppingZone.
5. Add entries for Pepperoni, Peppers, Olives, Onions, Pineapple, etc.

Important:
- This script uses FoodItem.toppings and does not change customer order logic.
- The pizza topping zone should not be the cheese object. This allows toppings to work on no-cheese pizzas.
- If a topping visually spawns but is sunken into the food, increase Surface Offset on the FoodToppingScatterZone.
