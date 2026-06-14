using UnityEngine;

public class PlayerFoodHands : MonoBehaviour
{
    [Header("Hold Setup")]
    public Transform holdPoint;
    public bool destroyHeldItemWhenConsumed = true;

    [Header("Held Item")]
    [SerializeField] private FoodItem heldFoodItem;

    public bool HasFood => heldFoodItem != null;
    public FoodItem HeldFoodItem => heldFoodItem;

    public bool TryGetHeldFood(out FoodItem foodItem)
    {
        foodItem = heldFoodItem;
        return foodItem != null;
    }

    public bool CanHoldFood()
    {
        return heldFoodItem == null;
    }

    public bool TryHoldExistingFood(FoodItem foodItem)
    {
        if (foodItem == null) return false;
        if (!CanHoldFood()) return false;

        heldFoodItem = foodItem;
        AttachToHoldPoint(foodItem);
        return true;
    }

    public FoodItem RemoveHeldFood(bool keepAtCurrentPosition = true)
    {
        if (heldFoodItem == null) return null;

        FoodItem item = heldFoodItem;
        heldFoodItem = null;

        item.transform.SetParent(null, true);
        SetPhysicsHeld(item, false);

        if (!keepAtCurrentPosition && holdPoint != null)
        {
            item.transform.position = holdPoint.position;
            item.transform.rotation = holdPoint.rotation;
        }

        return item;
    }

    public void ConsumeHeldFood()
    {
        if (heldFoodItem == null) return;

        FoodItem item = heldFoodItem;
        heldFoodItem = null;

        if (destroyHeldItemWhenConsumed)
            Destroy(item.gameObject);
        else
            item.gameObject.SetActive(false);
    }

    public void DropHeldFood(Vector3 dropVelocity)
    {
        FoodItem item = RemoveHeldFood();
        if (item == null) return;

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = dropVelocity;
    }

    private void AttachToHoldPoint(FoodItem foodItem)
    {
        if (holdPoint == null)
        {
            Debug.LogWarning($"{name} has no holdPoint assigned.", this);
            return;
        }

        foodItem.transform.SetParent(holdPoint, false);
        foodItem.transform.localPosition = Vector3.zero;
        foodItem.transform.localRotation = Quaternion.identity;
        SetPhysicsHeld(foodItem, true);
    }

    private void SetPhysicsHeld(FoodItem foodItem, bool held)
    {
        Rigidbody rb = foodItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = held;
            rb.useGravity = !held;
            if (held)
                rb.linearVelocity = Vector3.zero;
        }

        Collider[] colliders = foodItem.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
            col.enabled = !held;
    }
}
