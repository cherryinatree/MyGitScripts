using UnityEngine;
using UnityEngine.Events;

public class PizzaConveyorOven : MonoBehaviour
{
    [Header("Belt Points")]
    public Transform beltStartPoint;
    public Transform beltEndPoint;

    [Header("Cooking")]
    [Range(0.05f, 0.95f)] public float cookedAtProgress = 0.65f;
    public float beltSeconds = 18f;

    [Header("Pickup")]
    public bool cookedPizzaOnlyCanBePickedUp = true;

    [Header("Events")]
    public UnityEvent onPizzaPlaced;
    public UnityEvent onWrongItemTried;

    public bool TryPlacePizza(PlayerFoodHands hands)
    {
        if (hands == null) return false;
        if (!hands.TryGetHeldFood(out FoodItem item)) return false;

        if (item.foodKind != FoodKind.RawPizza)
        {
            onWrongItemTried?.Invoke();
            return false;
        }

        FoodItem pizza = hands.RemoveHeldFood();
        PlaceOnBelt(pizza);
        onPizzaPlaced?.Invoke();
        return true;
    }

    private void PlaceOnBelt(FoodItem pizza)
    {
        Transform start = beltStartPoint != null ? beltStartPoint : transform;
        pizza.transform.position = start.position;
        pizza.transform.rotation = start.rotation;
        pizza.transform.SetParent(null, true);

        PizzaBeltItem beltItem = pizza.GetComponent<PizzaBeltItem>();
        if (beltItem == null)
            beltItem = pizza.gameObject.AddComponent<PizzaBeltItem>();

        beltItem.Begin(this, pizza);
    }

    public Vector3 GetStartPosition()
    {
        return beltStartPoint != null ? beltStartPoint.position : transform.position;
    }

    public Vector3 GetEndPosition()
    {
        if (beltEndPoint != null) return beltEndPoint.position;
        return transform.position + transform.forward * 3f;
    }
}
