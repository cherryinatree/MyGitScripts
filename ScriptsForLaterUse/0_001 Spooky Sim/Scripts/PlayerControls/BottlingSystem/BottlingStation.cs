using Cherry.Inventory;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Stations/Bottling Station")]
public class BottlingStation : MonoBehaviour
{
    [System.Serializable]
    public class BottlingRecipe
    {
        public ItemDefinition inputItem;
        [Min(1)] public int inputPerBottle = 1;
        public GameObject bottledPrefab;
    }

    [Header("Recipes")]
    public List<BottlingRecipe> recipes = new();

    [Header("Production")]
    [Min(0.05f)] public float intervalSeconds = 1.0f;
    [Tooltip("Where bottles spawn from the station.")]
    public Transform spawnPoint;
    [Tooltip("Where finished bottles float to.")]
    public Transform finishedArea;
    [Min(0.05f)] public float travelSeconds = 0.6f;
    [Min(0f)] public float arcHeight = 0.2f;

    [Header("State (runtime)")]
    [SerializeField] private BottlingRecipe activeRecipe;
    [SerializeField] private int storedUnits; // total input units loaded into station

    private Coroutine _produceCo;

    // -------- API --------
    public IEnumerable<BottlingRecipe> AllRecipes => recipes;

    public bool HasActiveBatch => activeRecipe != null && storedUnits > 0;

    public bool BeginBatch(IInventoryOps inventory, ItemDefinition chosenItem)
    {
        var r = recipes.Find(x => x.inputItem == chosenItem);
        if (r == null || r.bottledPrefab == null)
        {
            Debug.LogWarning($"{name}: No recipe for {chosenItem?.name} or missing bottled prefab.");
            return false;
        }

        int available = inventory.Count(chosenItem);
        if (available <= 0)
        {
            Debug.Log($"{name}: Player has no {chosenItem?.name} to bottle.");
            return false;
        }

        int removed = inventory.RemoveAll(chosenItem);
        if (removed <= 0) return false;

        activeRecipe = r;
        storedUnits += removed;

        if (_produceCo == null)
            _produceCo = StartCoroutine(ProduceLoop());

        return true;
    }

    // Optional: cancel/clear batch
    public void ClearBatch()
    {
        activeRecipe = null;
        storedUnits = 0;
        if (_produceCo != null) { StopCoroutine(_produceCo); _produceCo = null; }
    }

    private IEnumerator ProduceLoop()
    {
        var wait = new WaitForSeconds(intervalSeconds);

        while (activeRecipe != null && storedUnits >= activeRecipe.inputPerBottle)
        {
            // Consume inputs for one bottle
            storedUnits -= activeRecipe.inputPerBottle;

            // Spawn bottle
            Vector3 start = spawnPoint ? spawnPoint.position : transform.position;
            var go = Instantiate(activeRecipe.bottledPrefab, start, Quaternion.identity);

            // Configure mover
            var mover = go.GetComponent<BottledProductMover>();
            if (mover == null) mover = go.AddComponent<BottledProductMover>();

            mover.travelDuration = travelSeconds;
            mover.arcHeight = arcHeight;
            mover.target = finishedArea;

            // Optionally: notify listeners here, play SFX, etc.

            yield return wait;
        }

        // out of inputs
        _produceCo = null;
        if (storedUnits <= 0) activeRecipe = null;
    }
}
