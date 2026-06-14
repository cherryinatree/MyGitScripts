Food Station Practice Patch

Add FoodStationPracticeMode to the parent of your ice cream stand or pizza station while testing.
It will:
- Let FoodItemCreator make cones without assigning a customer.
- Let PizzaBaseCreator make raw pizzas without requiring a current customer.
- Make stock/costs free or infinite, depending on the inspector settings.
- Make trash cans accept all food.
- Optionally allow pickup of uncooked pizza from the oven.

Add FoodTrashTrigger to a trash can object if you want to physically throw/drop food into the trash.
The trash can needs a Collider with Is Trigger enabled.
Food objects need colliders/rigidbodies enabled when dropped/thrown.

For normal gameplay, disable or remove FoodStationPracticeMode.
