using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Lets the player throw away held food.
/// Useful when they mess up sauce/cheese/toppings or grab the wrong item.
/// </summary>
public class FoodTrashCan : MonoBehaviour
{
    [Header("Rules")]
    public bool onlyAcceptRuinedFood;
    public bool allowIceCream = true;
    public bool allowPizza = true;
    public bool allowBatches = true;

    [Header("Events")]
    public UnityEvent onFoodTrashed;
    public UnityEvent onTrashRejected;

    public bool TryTrashHeldFood(PlayerFoodHands hands)
    {
        if (hands == null) return false;
        if (!hands.TryGetHeldFood(out FoodItem item)) return false;

        if (!CanTrash(item))
        {
            onTrashRejected?.Invoke();
            return false;
        }

        hands.ConsumeHeldFood();
        onFoodTrashed?.Invoke();
        return true;
    }

    private bool CanTrash(FoodItem item)
    {
        if (item == null) return false;
        if (onlyAcceptRuinedFood && !item.isRuined) return false;

        switch (item.foodKind)
        {
            case FoodKind.IceCreamCone:
            case FoodKind.IceCreamTub:
            case FoodKind.IceCreamMixture:
                return allowIceCream || allowBatches;

            case FoodKind.PizzaDoughBall:
                return allowBatches;

            case FoodKind.RawPizza:
            case FoodKind.CookedPizza:
                return allowPizza;

            default:
                return true;
        }
    }
}
