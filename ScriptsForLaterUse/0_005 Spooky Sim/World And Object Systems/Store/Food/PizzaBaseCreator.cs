using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Creates a raw pizza from the station dough stock.
/// Usually placed on the pizza station where the player grabs dough to start an order.
/// </summary>
public class PizzaBaseCreator : MonoBehaviour
{
    [Header("Creation")]
    public FoodItem rawPizzaPrefab;
    public Transform spawnPoint;

    [Header("Dough Stock")]
    public FoodStationInventory stationInventory;
    public FoodStockType doughStockType = FoodStockType.PizzaDough;
    public int doughCostPerPizza = 1;

    [Header("Order Assignment")]
    public FoodCustomerOrder currentCustomer;
    public FoodStationLineController lineController;
    public bool requireCurrentCustomer = true;
    public bool assignPizzaToCurrentCustomer = true;

    [Header("Events")]
    public UnityEvent onPizzaCreated;
    public UnityEvent onNoDough;
    public UnityEvent onNoCustomer;
    public UnityEvent onHandsFull;

    public bool TryCreatePizzaIntoHands(PlayerFoodHands hands)
    {
        if (hands == null) return false;
        if (!hands.CanHoldFood())
        {
            onHandsFull?.Invoke();
            return false;
        }

        FoodCustomerOrder customer = GetCurrentCustomer();
        if (requireCurrentCustomer && customer == null)
        {
            onNoCustomer?.Invoke();
            return false;
        }

        if (rawPizzaPrefab == null) return false;

        if (stationInventory != null && doughCostPerPizza > 0)
        {
            if (!stationInventory.TryConsume(doughStockType, doughCostPerPizza))
            {
                onNoDough?.Invoke();
                return false;
            }
        }

        Transform point = spawnPoint != null ? spawnPoint : transform;
        FoodItem pizza = Instantiate(rawPizzaPrefab, point.position, point.rotation);
        pizza.foodKind = FoodKind.RawPizza;

        if (assignPizzaToCurrentCustomer && customer != null)
            pizza.AssignToCustomer(customer);

        hands.TryHoldExistingFood(pizza);
        onPizzaCreated?.Invoke();
        return true;
    }

    private FoodCustomerOrder GetCurrentCustomer()
    {
        if (lineController != null && lineController.CurrentCustomerOrder != null)
            return lineController.CurrentCustomerOrder;

        return currentCustomer;
    }
}
