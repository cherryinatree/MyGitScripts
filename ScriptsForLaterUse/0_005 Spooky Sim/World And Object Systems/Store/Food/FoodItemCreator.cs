using UnityEngine;

public class FoodItemCreator : MonoBehaviour
{
    [Header("Creation")]
    public FoodItem foodPrefab;
    public Transform spawnPoint;
    public FoodStationInventory inventory;
    public FoodStockType requiredStockType;
    public int stockCost = 0;

    [Header("Order Assignment")]
    public FoodCustomerOrder currentCustomer;
    public bool assignCreatedFoodToCustomer;

    public bool TryCreateIntoHands(PlayerFoodHands hands)
    {
        if (hands == null) return false;
        if (!hands.CanHoldFood()) return false;
        if (foodPrefab == null) return false;

        if (inventory != null && stockCost > 0)
        {
            if (!inventory.TryConsume(requiredStockType, stockCost))
            {
                Debug.Log($"Not enough {requiredStockType} to create {foodPrefab.name}.", this);
                return false;
            }
        }

        Transform point = spawnPoint != null ? spawnPoint : transform;
        FoodItem created = Instantiate(foodPrefab, point.position, point.rotation);

        if (assignCreatedFoodToCustomer && currentCustomer != null)
            created.AssignToCustomer(currentCustomer);

        hands.TryHoldExistingFood(created);
        return true;
    }
}
