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

    private float _lastSpawnTime = -999f;

    private void Start()
    {
        orgionalQuantity = quantity;
    
    }

    public bool TryHarvestFromBeam(Vector3 hitPoint, Transform beamOrigin)
    {
        
        if (quantity < perPickup) return false;
        if (pickupPrefab == null) { Debug.LogWarning($"{name}: No pickupPrefab assigned."); return false; }
        if (Time.time - _lastSpawnTime < spawnCooldown) return false;

        _lastSpawnTime = Time.time;

        // Where to spawn
        var spawnPos = hitPoint + transform.TransformVector(localSpawnOffset);
        var go = Instantiate(pickupPrefab, spawnPos, Quaternion.identity);

        var mover = go.GetComponent<PickupMover>();
        if (mover == null)
        {
            Debug.LogError($"{name}: pickupPrefab requires a PickupMover component.");
            Destroy(go);
            return false;
        }

        // Optionally reserve immediately to avoid over-harvest spam
        if (reserveOnSpawn) quantity -= perPickup;

        mover.Initialize(
            item: item,
            amount: perPickup,
            target: beamOrigin,
            onArrive: (success) =>
            {
                if (success)
                {
                    // If we didn't reserve earlier, decrement now
                    if (!reserveOnSpawn)
                    {
                        quantity -= perPickup;
                        float newScale = 1 * ((float)quantity / (float)orgionalQuantity);
                        transform.localScale = new Vector3(newScale, newScale,newScale);
                        if(quantity <= 0)
                        {
                            Destroy(gameObject);
                        }
                    }

                    // If this source is depleted, you can disable it / change visual state here
                    // e.g., if (quantity <= 0) GetComponent<Collider>().enabled = false;
                }
                else
                {
                    // If arrival failed and we had reserved on spawn, refund
                    if (reserveOnSpawn) quantity += perPickup;
                }
            }
        );

        return true;
    }
}
