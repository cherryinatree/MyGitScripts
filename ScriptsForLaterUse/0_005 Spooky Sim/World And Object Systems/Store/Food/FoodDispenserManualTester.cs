using UnityEngine;

/// <summary>
/// Temporary setup helper. Put this on an ingredient bin and use the component's context menu
/// "TEST: Use Dispenser On Player Hands" during Play Mode.
/// If the context menu works but normal interact does not, your interaction system is not reaching InteractRelay.
/// </summary>
public class FoodDispenserManualTester : MonoBehaviour
{
    public IngredientDispenser dispenser;
    public PlayerFoodHands hands;

    private void Reset()
    {
        dispenser = GetComponent<IngredientDispenser>();
    }

    [ContextMenu("TEST: Use Dispenser On Player Hands")]
    public void TestUseDispenserOnPlayerHands()
    {
        if (dispenser == null)
            dispenser = GetComponent<IngredientDispenser>();

        if (hands == null)
            hands = FindFirstObjectByType<PlayerFoodHands>();

        if (dispenser == null)
        {
            Debug.LogWarning($"{name}: No IngredientDispenser found on this object.", this);
            return;
        }

        if (hands == null)
        {
            Debug.LogWarning($"{name}: No PlayerFoodHands found in scene.", this);
            return;
        }

        bool worked = dispenser.TryUse(hands);
        Debug.Log($"{name}: Manual dispenser test result = {worked}", this);
    }
}
