using UnityEngine;
using UnityEngine.Events;

public class FoodHandOffZone : MonoBehaviour
{
    [Header("Customer")]
    public FoodCustomerOrder customer;

    [Header("Behavior")]
    public bool consumeCorrectFood = true;
    public bool consumeWrongFood = false;

    [Header("Events")]
    public UnityEvent onCorrectFoodHandedOver;
    public UnityEvent onWrongFoodHandedOver;

    public bool TryHandOver(PlayerFoodHands hands)
    {
        if (hands == null) return false;
        if (customer == null) return false;
        if (!hands.TryGetHeldFood(out FoodItem foodItem)) return false;

        bool accepted = customer.TryReceiveFood(foodItem);

        if (accepted)
        {
            if (consumeCorrectFood)
                hands.ConsumeHeldFood();

            onCorrectFoodHandedOver?.Invoke();
            return true;
        }

        if (consumeWrongFood)
            hands.ConsumeHeldFood();

        onWrongFoodHandedOver?.Invoke();
        return false;
    }
}
