using UnityEngine;

/// <summary>
/// Bridge for your existing interaction system. This version auto-finds components on the same GameObject
/// and can print why an interaction did or did not do anything.
/// </summary>
public class InteractRelay : MonoBehaviour
{
    [Header("Optional References")]
    public PlayerFoodHands hands;

    [Header("Creation / Assembly")]
    public FoodItemCreator foodItemCreator;
    public PizzaBaseCreator pizzaBaseCreator;
    public IngredientDispenser ingredientDispenser;

    [Header("Customer / Handoff")]
    public FoodHandOffZone handOffZone;

    [Header("Batch / Processing")]
    public FoodRecipeIngredientButton recipeIngredientButton;
    public FoodRecipeBatchMaker batchMaker;
    public FreezerProcessor freezerProcessor;
    public PizzaDoughArea pizzaDoughArea;
    public IceCreamTubSlot iceCreamTubSlot;

    [Header("Pizza Oven")]
    public PizzaConveyorOven pizzaConveyorOven;
    public PizzaBeltItem pizzaBeltItem;

    [Header("Cleanup")]
    public FoodTrashCan trashCan;

    [Header("Debug")]
    public bool autoFindComponentsOnThisObject = true;
    public bool verboseDebug = true;

    private void Reset()
    {
        AutoFindLocalComponents();
    }

    private void Awake()
    {
        if (autoFindComponentsOnThisObject)
            AutoFindLocalComponents();
    }

    private void AutoFindLocalComponents()
    {
        if (foodItemCreator == null) foodItemCreator = GetComponent<FoodItemCreator>();
        if (pizzaBaseCreator == null) pizzaBaseCreator = GetComponent<PizzaBaseCreator>();
        if (ingredientDispenser == null) ingredientDispenser = GetComponent<IngredientDispenser>();
        if (handOffZone == null) handOffZone = GetComponent<FoodHandOffZone>();
        if (recipeIngredientButton == null) recipeIngredientButton = GetComponent<FoodRecipeIngredientButton>();
        if (batchMaker == null) batchMaker = GetComponent<FoodRecipeBatchMaker>();
        if (freezerProcessor == null) freezerProcessor = GetComponent<FreezerProcessor>();
        if (pizzaDoughArea == null) pizzaDoughArea = GetComponent<PizzaDoughArea>();
        if (iceCreamTubSlot == null) iceCreamTubSlot = GetComponent<IceCreamTubSlot>();
        if (pizzaConveyorOven == null) pizzaConveyorOven = GetComponent<PizzaConveyorOven>();
        if (pizzaBeltItem == null) pizzaBeltItem = GetComponent<PizzaBeltItem>();
        if (trashCan == null) trashCan = GetComponent<FoodTrashCan>();
    }

    public void Interact()
    {
        if (autoFindComponentsOnThisObject)
            AutoFindLocalComponents();

        if (hands == null)
            hands = FindFirstObjectByType<PlayerFoodHands>();

        if (hands == null)
        {
            Debug.LogWarning($"{name}: InteractRelay could not find PlayerFoodHands.", this);
            return;
        }

        if (verboseDebug)
        {
            string held = hands.TryGetHeldFood(out FoodItem heldFood)
                ? heldFood.DebugDescription()
                : "nothing";
            Debug.Log($"{name}: InteractRelay called. Player is holding: {held}", this);
        }

        if (foodItemCreator != null && foodItemCreator.TryCreateIntoHands(hands)) { Success("FoodItemCreator"); return; }
        if (pizzaBaseCreator != null && pizzaBaseCreator.TryCreatePizzaIntoHands(hands)) { Success("PizzaBaseCreator"); return; }
        if (ingredientDispenser != null && ingredientDispenser.TryUse(hands)) { Success("IngredientDispenser"); return; }
        if (handOffZone != null && handOffZone.TryHandOver(hands)) { Success("FoodHandOffZone"); return; }
        if (recipeIngredientButton != null && recipeIngredientButton.TryAddIngredient()) { Success("FoodRecipeIngredientButton"); return; }
        if (batchMaker != null && batchMaker.TryCraft(hands)) { Success("FoodRecipeBatchMaker"); return; }
        if (freezerProcessor != null && freezerProcessor.TryInteract(hands)) { Success("FreezerProcessor"); return; }
        if (pizzaDoughArea != null && pizzaDoughArea.TryLoadDough(hands)) { Success("PizzaDoughArea"); return; }
        if (iceCreamTubSlot != null && iceCreamTubSlot.TryLoadTub(hands)) { Success("IceCreamTubSlot"); return; }
        if (pizzaConveyorOven != null && pizzaConveyorOven.TryPlacePizza(hands)) { Success("PizzaConveyorOven"); return; }
        if (pizzaBeltItem != null && pizzaBeltItem.TryPickup(hands)) { Success("PizzaBeltItem"); return; }
        if (trashCan != null && trashCan.TryTrashHeldFood(hands)) { Success("FoodTrashCan"); return; }

        if (verboseDebug)
        {
            Debug.LogWarning($"{name}: InteractRelay was called, but no assigned component accepted the interaction. If this is a scoop bin, make sure IngredientDispenser is on this object and actionType/flavor are set.", this);
        }
    }

    private void Success(string componentName)
    {
        if (verboseDebug)
            Debug.Log($"{name}: Interaction handled by {componentName}.", this);
    }
}
