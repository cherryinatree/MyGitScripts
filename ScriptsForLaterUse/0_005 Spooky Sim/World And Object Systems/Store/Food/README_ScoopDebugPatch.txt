Replace InteractRelay.cs and IngredientDispenser.cs with these versions.

What this patch does:
1. InteractRelay now auto-finds IngredientDispenser/FoodItemCreator/etc. on the same GameObject.
2. IngredientDispenser now prints the exact reason a scoop failed.
3. Add FoodDispenserManualTester to a bin if you want to test the bin from its component context menu.

Test steps:
1. Put the updated scripts in Unity.
2. Let Unity compile.
3. Grab a cone.
4. Interact with the chocolate bin.
5. Read the Console.

If you see no InteractRelay log at all, your interaction system is not calling InteractRelay on the bin.
If InteractRelay logs but the dispenser fails, the IngredientDispenser warning will tell you why.
