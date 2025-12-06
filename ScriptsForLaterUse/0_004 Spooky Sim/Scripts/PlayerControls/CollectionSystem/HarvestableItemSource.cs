using Cherry.Inventory;
using UnityEngine;

[AddComponentMenu("Items/Harvestable Item Source")]
public class HarvestableItemSource : MonoBehaviour
{
    [Header("Item")]
    public ItemDefinition item;          // your existing ScriptableObject
    [Min(0)] public int quantity = 5;    // how many units available total
    [Min(1)] public int perPickup = 1;   // amount granted per successful pickup

    private int orgionalQuantity;

    [Header("Pickup Prefab")]
    [Tooltip("Prefab that will fly to the player. Must have a PickupMover component.")]
    public GameObject pickupPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Optional visual spawn offset from the hit point.")]
    public Vector3 localSpawnOffset = Vector3.zero;

    [Tooltip("If true, we reserve (decrement) on spawn; if false, we only decrement on arrival.")]
    public bool reserveOnSpawn = false;

    [Tooltip("Optional cooldown between spawns from this source (seconds).")]
    [Min(0)] public float spawnCooldown = 0.1f;

    [Header("Visuals / Lifetime")]
    [Tooltip("If true, object scales down with remaining quantity.")]
    public bool scaleWithQuantity = true;

    [Tooltip("If true, destroy this source when quantity hits 0.")]
    public bool destroyWhenDepleted = true;

    private float _lastSpawnTime = -999f;

    private void Start()
    {
        orgionalQuantity = Mathf.Max(1, quantity);
    }

    public bool TryHarvestFromBeam(Vector3 hitPoint, Transform beamOrigin)
    {
        if (quantity < perPickup) return false;
        if (pickupPrefab == null) { Debug.LogWarning($"{name}: No pickupPrefab assigned."); return false; }
        if (Time.time - _lastSpawnTime < spawnCooldown) return false;

        _lastSpawnTime = Time.time;

        // Spawn position
        var spawnPos = hitPoint + transform.TransformVector(localSpawnOffset);
        var go = Instantiate(pickupPrefab, spawnPos, Quaternion.identity);

        var mover = go.GetComponent<PickupMover>();
        if (mover == null)
        {
            Debug.LogError($"{name}: pickupPrefab requires a PickupMover component.");
            Destroy(go);
            return false;
        }

        // Reserve immediately (optional)
        if (reserveOnSpawn)
        {
            quantity -= perPickup;
            ApplyScale();
            if (destroyWhenDepleted && quantity <= 0)
            {
                // Object can be destroyed BEFORE mover arrives -> callback MUST guard against it
                Destroy(gameObject);
            }
        }

        mover.Initialize(
            item: item,
            amount: perPickup,
            target: beamOrigin,
            onArrive: (success) =>
            {
                // ---- CRITICAL GUARD ----
                // If this source got destroyed (common when reserveOnSpawn + depleted, or other cleanup),
                // do nothing: prevents MissingReferenceException on transform access.
                if (!this) return;

                if (success)
                {
                    // If we didn't reserve earlier, decrement now
                    if (!reserveOnSpawn)
                    {
                        quantity -= perPickup;
                        ApplyScale();
                        if (destroyWhenDepleted && quantity <= 0)
                            Destroy(gameObject);
                    }
                }
                else
                {
                    // If arrival failed and we had reserved on spawn, refund
                    if (reserveOnSpawn)
                    {
                        quantity += perPickup;
                        ApplyScale();
                    }
                }
            }
        );

        return true;
    }

    private void ApplyScale()
    {
        if (!scaleWithQuantity) return;
        if (orgionalQuantity <= 0) return;

        float t = Mathf.Clamp01((float)quantity / orgionalQuantity);
        transform.localScale = Vector3.one * t;
    }
}
