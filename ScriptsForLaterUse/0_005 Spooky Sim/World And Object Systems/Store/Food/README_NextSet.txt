Food Station Scripts - Next Set

Copy these scripts into the same Unity folder as the first FoodStationScripts pack.
This pack adds customer line/waiting behavior, pizza dough handling, order displays, money payout events, trash cleanup, held item debug text, and food visuals.

Important overwrite:
- InteractRelay.cs is an updated version. Replace the old InteractRelay.cs with this one.

Suggested pizza setup:
1. Pizza Station object:
   - FoodStationLineController
   - PizzaBaseCreator
   - FoodOrderDisplayPanel

2. Pizza dough area:
   - PizzaDoughArea
   - InteractRelay with pizzaDoughArea assigned

3. Pizza base grab spot:
   - PizzaBaseCreator
   - InteractRelay with pizzaBaseCreator assigned

4. Pizza oven:
   - PizzaConveyorOven
   - InteractRelay with pizzaConveyorOven assigned
   - Wire PizzaConveyorOven.onPizzaPlaced to FoodStationLineController.SendCurrentCustomerToPizzaWaiting()

5. Customer prefab:
   - FoodCustomerOrder
   - FoodCustomerMover
   - FoodCustomerStationAgent
   - FoodMoneyPayout optional
   - Wire FoodCustomerOrder.onOrderAccepted to:
       FoodCustomerStationAgent.HandleOrderAccepted()
       FoodMoneyPayout.PayCustomer()
   - Wire FoodCustomerOrder.onOrderFailed to:
       FoodCustomerStationAgent.HandleOrderFailed()

6. Pizza customers:
   - Set FoodCustomerStationAgent.station to your pizza station line controller.
   - Either let registerOnStart handle it, or call RegisterWithStation() when your customer reaches the station.
   - If using the line controller, consider turning FoodCustomerOrder.generateOrderOnStart off so orders generate when served.

Suggested ice cream setup:
1. Ice cream station object:
   - FoodStationLineController set to IceCream
   - FoodOrderDisplayPanel

2. Customer prefab:
   - Same customer setup as pizza, but stationOrderType should be IceCream.
   - Ice cream customers stay at the counter until they receive the cone.

3. Cone creator:
   - FoodItemCreator
   - InteractRelay with foodItemCreator assigned
   - Optional: assign the ice cream station line controller's itemCreatorToSync to this creator.

Visuals:
- Put FoodItemStateVisuals on pizza/cone prefabs if you want child visuals to toggle automatically.
- Assign child objects for sauce, cheese, scoop flavor, toppings, cooked, and ruined states.

Money:
- FoodMoneyPayout does not directly know your money script.
- Use onMoneyPaid(int amount) in the inspector to call your existing money/store report method.
