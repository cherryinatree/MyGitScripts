using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns visual topping models when toppings are added to a FoodItem.
/// Works for ice cream cones and pizzas.
///
/// Ice cream: assign one scatter zone per scoop slot. The script uses the current top scoop.
/// Pizza: assign a pizza topping zone independent of cheese, so no-cheese pizzas still get toppings in the right area.
/// </summary>
public class FoodToppingScatterVisuals : MonoBehaviour
{
    [Serializable]
    public class ToppingVisualEntry
    {
        public FoodIngredient topping = FoodIngredient.None;

        [Tooltip("One or more prefab variants for this topping. The script randomly picks one each piece.")]
        public GameObject[] prefabs;

        [Tooltip("How many little pieces to spawn each time this topping is added.")]
        public int piecesPerUse = 6;

        [Tooltip("Random uniform scale range. X = min, Y = max.")]
        public Vector2 randomScale = new Vector2(1f, 1f);

        [Tooltip("Extra local rotation applied after surface alignment/random yaw.")]
        public Vector3 extraEulerRotation;

        [Tooltip("If true, spawned pieces become children of the chosen scatter zone.")]
        public bool parentToZone = true;
    }

    [Header("References")]
    public FoodItem foodItem;

    [Tooltip("For ice cream, assign bottom/middle/top scoop scatter zones in that order.")]
    public FoodToppingScatterZone[] iceCreamScoopZones = new FoodToppingScatterZone[3];

    [Tooltip("For pizza, assign an invisible area above the pizza where toppings should land. This is not the cheese object.")]
    public FoodToppingScatterZone pizzaToppingZone;

    [Tooltip("Optional fallback parent for spawned topping objects.")]
    public Transform fallbackSpawnParent;

    [Header("Topping Prefabs")]
    public List<ToppingVisualEntry> toppingVisuals = new List<ToppingVisualEntry>();

    [Header("Behavior")]
    [Tooltip("Checks for newly added toppings every frame. Keep on unless you manually call RefreshNow after modifying FoodItem.toppings.")]
    public bool refreshEveryFrame = true;

    [Tooltip("If the FoodItem toppings list is reduced, rebuild the spawned visuals to match.")]
    public bool rebuildIfToppingListShrinks = true;

    [Tooltip("For ice cream, if the player adds another scoop after adding toppings, move/rebuild toppings onto the new top scoop.")]
    public bool rebuildIceCreamToppingsWhenTopScoopChanges = true;

    [Tooltip("Useful if you want no visual duplicates when old topping models already exist on the prefab.")]
    public bool clearOnStart = true;

    [Header("Debug")]
    public bool verboseDebug;

    private readonly List<GameObject> spawnedPieces = new List<GameObject>();
    private int renderedToppingCount;
    private int lastRenderedIceCreamTopScoopIndex = -1;

    private void Reset()
    {
        foodItem = GetComponent<FoodItem>();
        fallbackSpawnParent = transform;
    }

    private void Awake()
    {
        if (foodItem == null)
            foodItem = GetComponent<FoodItem>();

        if (fallbackSpawnParent == null)
            fallbackSpawnParent = transform;
    }

    private void Start()
    {
        if (clearOnStart)
            ClearSpawnedToppings();

        RefreshNow();
    }

    private void Update()
    {
        if (refreshEveryFrame)
            RefreshNow();
    }

    [ContextMenu("Refresh Topping Visuals Now")]
    public void RefreshNow()
    {
        if (foodItem == null || foodItem.toppings == null)
            return;

        if (rebuildIfToppingListShrinks && renderedToppingCount > foodItem.toppings.Count)
        {
            ClearSpawnedToppings();
        }

        if (rebuildIceCreamToppingsWhenTopScoopChanges && foodItem.foodKind == FoodKind.IceCreamCone)
        {
            int currentTopIndex = foodItem.GetTotalIceCreamScoops() - 1;
            if (foodItem.toppings.Count > 0 && renderedToppingCount > 0 && currentTopIndex != lastRenderedIceCreamTopScoopIndex)
            {
                ClearSpawnedToppings();
            }
        }

        while (renderedToppingCount < foodItem.toppings.Count)
        {
            FoodIngredient topping = foodItem.toppings[renderedToppingCount];
            SpawnVisualsForTopping(topping);
            renderedToppingCount++;
        }

        if (foodItem.foodKind == FoodKind.IceCreamCone)
            lastRenderedIceCreamTopScoopIndex = foodItem.GetTotalIceCreamScoops() - 1;
    }

    [ContextMenu("Clear Spawned Toppings")]
    public void ClearSpawnedToppings()
    {
        for (int i = spawnedPieces.Count - 1; i >= 0; i--)
        {
            GameObject piece = spawnedPieces[i];
            if (piece == null) continue;

            if (Application.isPlaying)
                Destroy(piece);
            else
                DestroyImmediate(piece);
        }

        spawnedPieces.Clear();
        renderedToppingCount = 0;
        lastRenderedIceCreamTopScoopIndex = foodItem != null && foodItem.foodKind == FoodKind.IceCreamCone
            ? foodItem.GetTotalIceCreamScoops() - 1
            : -1;
    }

    [ContextMenu("Rebuild Spawned Toppings")]
    public void RebuildSpawnedToppings()
    {
        ClearSpawnedToppings();
        RefreshNow();
    }

    private void SpawnVisualsForTopping(FoodIngredient topping)
    {
        if (topping == FoodIngredient.None)
            return;

        ToppingVisualEntry visual = FindVisualEntry(topping);
        if (visual == null || visual.prefabs == null || visual.prefabs.Length == 0)
        {
            if (verboseDebug)
                Debug.LogWarning($"{name}: No topping prefab assigned for {topping}.", this);
            return;
        }

        FoodToppingScatterZone zone = GetCurrentScatterZone();
        if (zone == null)
        {
            if (verboseDebug)
                Debug.LogWarning($"{name}: No scatter zone available for {foodItem.foodKind}.", this);
            return;
        }

        int pieces = Mathf.Max(1, visual.piecesPerUse);
        for (int i = 0; i < pieces; i++)
        {
            GameObject prefab = visual.prefabs[UnityEngine.Random.Range(0, visual.prefabs.Length)];
            if (prefab == null) continue;

            zone.TryGetRandomPlacement(out Vector3 position, out Quaternion rotation);
            rotation *= Quaternion.Euler(visual.extraEulerRotation);

            Transform parent = visual.parentToZone ? zone.transform : fallbackSpawnParent;
            GameObject instance = Instantiate(prefab, position, rotation, parent);

            float minScale = Mathf.Min(visual.randomScale.x, visual.randomScale.y);
            float maxScale = Mathf.Max(visual.randomScale.x, visual.randomScale.y);
            float scale = UnityEngine.Random.Range(minScale, maxScale);
            instance.transform.localScale *= scale;

            spawnedPieces.Add(instance);
        }
    }

    private FoodToppingScatterZone GetCurrentScatterZone()
    {
        if (foodItem == null) return null;

        if (foodItem.foodKind == FoodKind.IceCreamCone)
        {
            int totalScoops = foodItem.GetTotalIceCreamScoops();
            if (totalScoops <= 0)
                return null;

            int scoopIndex = Mathf.Clamp(totalScoops - 1, 0, iceCreamScoopZones.Length - 1);
            return iceCreamScoopZones[scoopIndex];
        }

        if (foodItem.foodKind == FoodKind.RawPizza || foodItem.foodKind == FoodKind.CookedPizza)
            return pizzaToppingZone;

        return null;
    }

    private ToppingVisualEntry FindVisualEntry(FoodIngredient topping)
    {
        for (int i = 0; i < toppingVisuals.Count; i++)
        {
            ToppingVisualEntry entry = toppingVisuals[i];
            if (entry != null && entry.topping == topping)
                return entry;
        }

        return null;
    }
}
