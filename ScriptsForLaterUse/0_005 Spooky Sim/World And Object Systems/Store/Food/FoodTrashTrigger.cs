using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Trigger-based trash can for food items.
/// Add this to a trash can object with a trigger collider, then physically drop/throw food into it.
/// </summary>
[RequireComponent(typeof(Collider))]
public class FoodTrashTrigger : MonoBehaviour
{
    [Header("Rules")]
    public bool onlyAcceptRuinedFood;
    public bool allowIceCream = true;
    public bool allowPizza = true;
    public bool allowBatches = true;

    [Header("Behavior")]
    public bool destroyFood = true;
    public bool deactivateFoodInstead;

    [Header("Events")]
    public UnityEvent onFoodTrashed;
    public UnityEvent onTrashRejected;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        FoodItem item = other.GetComponentInParent<FoodItem>();
        if (item == null)
            item = other.GetComponentInChildren<FoodItem>();

        if (item == null)
            return;

        TryTrash(item);
    }

    public bool TryTrash(FoodItem item)
    {
        if (item == null) return false;

        if (!CanTrash(item))
        {
            onTrashRejected?.Invoke();
            return false;
        }

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (destroyFood)
            Destroy(item.gameObject);
        else if (deactivateFoodInstead)
            item.gameObject.SetActive(false);

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
