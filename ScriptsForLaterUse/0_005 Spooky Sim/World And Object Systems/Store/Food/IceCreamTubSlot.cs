using UnityEngine;
using UnityEngine.Events;

public class IceCreamTubSlot : MonoBehaviour
{
    [Header("Slot")]
    public IceCreamFlavor acceptedFlavor = IceCreamFlavor.Chocolate;
    public FoodStationInventory stationInventory;
    public int servingsAdded = 10;

    [Header("Events")]
    public UnityEvent onTubAccepted;
    public UnityEvent onTubRejected;

    public bool TryLoadTub(PlayerFoodHands hands)
    {
        if (hands == null) return false;
        if (stationInventory == null) return false;
        if (!hands.TryGetHeldFood(out FoodItem item)) return false;

        if (item.foodKind != FoodKind.IceCreamTub || item.iceCreamFlavor != acceptedFlavor)
        {
            onTubRejected?.Invoke();
            return false;
        }

        stationInventory.Add(GetStockType(acceptedFlavor), servingsAdded);
        hands.ConsumeHeldFood();
        onTubAccepted?.Invoke();
        return true;
    }

    private FoodStockType GetStockType(IceCreamFlavor flavor)
    {
        return flavor switch
        {
            IceCreamFlavor.Chocolate => FoodStockType.ChocolateIceCream,
            IceCreamFlavor.Vanilla => FoodStockType.VanillaIceCream,
            IceCreamFlavor.Strawberry => FoodStockType.StrawberryIceCream,
            _ => FoodStockType.ChocolateIceCream
        };
    }
}
