using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class FreezerProcessor : MonoBehaviour
{
    [Header("Slot")]
    public Transform slotPoint;
    public FoodKind acceptedInputKind = FoodKind.IceCreamMixture;
    public FoodKind outputKind = FoodKind.IceCreamTub;

    [Header("Timing")]
    public float processSeconds = 20f;

    [Header("State")]
    [SerializeField] private FoodItem storedItem;
    [SerializeField] private bool isProcessing;
    [SerializeField] private bool isReady;

    [Header("Events")]
    public UnityEvent onItemInserted;
    public UnityEvent onProcessingFinished;
    public UnityEvent onItemRemoved;

    public bool IsOccupied => storedItem != null;
    public bool IsReady => isReady;

    public bool TryInteract(PlayerFoodHands hands)
    {
        if (hands == null) return false;

        if (!IsOccupied)
            return TryInsert(hands);

        if (isReady && hands.CanHoldFood())
            return TryRemove(hands);

        return false;
    }

    public bool TryInsert(PlayerFoodHands hands)
    {
        if (hands == null) return false;
        if (!hands.TryGetHeldFood(out FoodItem item)) return false;
        if (item.foodKind != acceptedInputKind) return false;

        storedItem = hands.RemoveHeldFood();
        PlaceStoredItem();

        isReady = false;
        StartCoroutine(ProcessRoutine());
        onItemInserted?.Invoke();
        return true;
    }

    public bool TryRemove(PlayerFoodHands hands)
    {
        if (hands == null) return false;
        if (storedItem == null) return false;
        if (!isReady) return false;
        if (!hands.CanHoldFood()) return false;

        FoodItem item = storedItem;
        storedItem = null;
        isReady = false;
        isProcessing = false;

        hands.TryHoldExistingFood(item);
        onItemRemoved?.Invoke();
        return true;
    }

    private IEnumerator ProcessRoutine()
    {
        isProcessing = true;
        yield return new WaitForSeconds(processSeconds);

        if (storedItem != null)
        {
            storedItem.foodKind = outputKind;
            isReady = true;
            onProcessingFinished?.Invoke();
        }

        isProcessing = false;
    }

    private void PlaceStoredItem()
    {
        if (storedItem == null) return;

        Transform point = slotPoint != null ? slotPoint : transform;
        storedItem.transform.SetParent(point, false);
        storedItem.transform.localPosition = Vector3.zero;
        storedItem.transform.localRotation = Quaternion.identity;

        Rigidbody rb = storedItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
        }
    }
}
