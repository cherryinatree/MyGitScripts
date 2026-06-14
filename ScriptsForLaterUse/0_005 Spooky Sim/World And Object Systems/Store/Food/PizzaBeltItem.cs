using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(FoodItem))]
public class PizzaBeltItem : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField] private PizzaConveyorOven oven;
    [SerializeField] private FoodItem foodItem;
    [SerializeField] private float progress;
    [SerializeField] private bool hasCooked;
    [SerializeField] private bool hasFallen;

    [Header("Fall")]
    public float fallForwardForce = 1.5f;
    public float fallDownForce = 1f;

    [Header("Events")]
    public UnityEvent onCooked;
    public UnityEvent onFellOff;
    public UnityEvent onPickedUp;

    public bool IsCooked => hasCooked;
    public bool HasFallen => hasFallen;

    public void Begin(PizzaConveyorOven newOven, FoodItem item)
    {
        oven = newOven;
        foodItem = item != null ? item : GetComponent<FoodItem>();
        progress = 0f;
        hasCooked = false;
        hasFallen = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
            col.enabled = true;
    }

    private void Update()
    {
        if (oven == null || foodItem == null || hasFallen) return;

        float seconds = Mathf.Max(0.01f, oven.beltSeconds);
        progress += Time.deltaTime / seconds;

        Vector3 start = oven.GetStartPosition();
        Vector3 end = oven.GetEndPosition();
        transform.position = Vector3.Lerp(start, end, progress);

        if (!hasCooked && progress >= oven.cookedAtProgress)
            CookPizza();

        if (progress >= 1f)
            FallOffBelt();
    }

    public bool TryPickup(PlayerFoodHands hands)
    {
        if (hands == null) return false;
        if (hasFallen) return false;
        if (!hands.CanHoldFood()) return false;
        if (oven != null && oven.cookedPizzaOnlyCanBePickedUp && !hasCooked) return false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
        }

        enabled = false;
        hands.TryHoldExistingFood(foodItem);
        onPickedUp?.Invoke();
        return true;
    }

    private void CookPizza()
    {
        hasCooked = true;
        foodItem.MarkCooked();
        onCooked?.Invoke();
    }

    private void FallOffBelt()
    {
        hasFallen = true;
        foodItem.MarkRuined();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = oven.transform.forward * fallForwardForce + Vector3.down * fallDownForce;
        }

        if (foodItem.assignedCustomer != null)
            foodItem.assignedCustomer.MarkOrderFailed();

        onFellOff?.Invoke();
        enabled = false;
    }
}
