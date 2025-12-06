using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cherry.Inventory;

[AddComponentMenu("Stations/Crafting Station")]
public class CraftingStation : MonoBehaviour
{
    [Serializable]
    public class StoredItem
    {
        public ItemDefinition item;
        public int amount;
    }

    [Header("Category & Recipes")]
    public CraftingCategory category = CraftingCategory.Bottling;
    [Tooltip("Only recipes of matching category will be used.")]
    public List<CraftingRecipeSO> recipes = new();

    [Header("Input Limits")]
    [Tooltip("Crafting grid is 4 slots => max 4 unique item types loaded at a time.")]
    [Min(1)] public int maxUniqueInputs = 4;

    [Tooltip("If enabled, players can only load items that appear in at least one recipe for this station.")]
    public bool restrictInputsToKnownIngredients = true;

    [Header("Production")]
    [Min(0.05f)] public float defaultIntervalSeconds = 1.0f;
    public Transform spawnPoint;
    public Transform finishedArea;
    [Min(0.05f)] public float travelSeconds = 0.6f;
    [Min(0f)] public float arcHeight = 0.2f;

    [Header("Runtime State (readonly)")]
    [SerializeField] private List<StoredItem> stored = new();
    [SerializeField] private CraftingRecipeSO activeRecipe;
    [SerializeField] private int plannedCrafts;
    [SerializeField] private int completedCrafts;

    private Coroutine _craftCo;

    public event Action<bool> OnBusyChanged;
    public event Action<float> OnProgress01Changed;
    public event Action OnStorageChanged;

    public bool IsBusy => _craftCo != null;
    public CraftingRecipeSO ActiveRecipe => activeRecipe;
    public IReadOnlyList<StoredItem> Stored => stored;

    // ------------------------- Storage Ops -------------------------

    public bool TryDeposit(IInventoryOps inventory, ItemDefinition item, int amount)
    {
        if (IsBusy) return false;
        if (item == null || amount <= 0) return false;
        if (restrictInputsToKnownIngredients && !IsKnownIngredient(item)) return false;

        int uniqueCount = CountUniqueTypes();
        var entry = FindStored(item);

        if (entry == null && uniqueCount >= maxUniqueInputs) return false;
        // Requires your IInventoryOps to support removing exact amounts.
        if (!inventory.TryRemove(item, amount)) return false;
        if (entry == null)
        {
            stored.Add(new StoredItem { item = item, amount = amount });
        }
        else
        {
            entry.amount += amount;
        }

        OnStorageChanged?.Invoke();
        return true;
    }

    public bool TryWithdraw(IInventoryOps inventory, ItemDefinition item, int amount)
    {
        if (IsBusy) return false;
        if (item == null || amount <= 0) return false;

        var entry = FindStored(item);
        if (entry == null || entry.amount < amount) return false;

        // Requires your IInventoryOps to support adding exact amounts.
        if (!inventory.TryAdd(item, amount)) return false;

        entry.amount -= amount;
        if (entry.amount <= 0)
            stored.Remove(entry);

        OnStorageChanged?.Invoke();
        return true;
    }

    public void EjectAllToPlayer(IInventoryOps inventory)
    {
        if (IsBusy) return;
        if (inventory == null) return;

        // Try to return everything; if inventory is full, leftovers remain in station.
        for (int i = stored.Count - 1; i >= 0; i--)
        {
            var s = stored[i];
            if (s.item == null || s.amount <= 0) { stored.RemoveAt(i); continue; }

            if (inventory.TryAdd(s.item, s.amount))
            {
                stored.RemoveAt(i);
            }
        }

        OnStorageChanged?.Invoke();
    }

    // ------------------------- Recipe Matching -------------------------

    public CraftingRecipeSO GetBestCraftableRecipe()
    {
        CraftingRecipeSO best = null;
        int bestCount = 0;

        foreach (var r in recipes)
        {
            if (r == null) continue;
            if (r.category != category) continue;
            if (r.outputPrefab == null) continue;

            int c = GetCraftableCount(r);
            if (c > bestCount)
            {
                bestCount = c;
                best = r;
            }
        }

        return bestCount > 0 ? best : null;
    }

    public int GetCraftableCount(CraftingRecipeSO recipe)
    {
        if (recipe == null) return 0;
        if (recipe.category != category) return 0;
        if (recipe.outputPrefab == null) return 0;
        if (recipe.inputs == null || recipe.inputs.Count == 0) return 0;

        int min = int.MaxValue;

        foreach (var ing in recipe.inputs)
        {
            if (ing.item == null || ing.amount <= 0) return 0;

            int have = CountStored(ing.item);
            int can = have / ing.amount;
            if (can < min) min = can;
            if (min == 0) return 0;
        }

        return min == int.MaxValue ? 0 : min;
    }

    // ------------------------- Crafting -------------------------

    public bool TryStartCrafting(CraftingRecipeSO recipe)
    {
        if (IsBusy) return false;
        if (recipe == null) return false;
        if (recipe.category != category) return false;

        int craftable = GetCraftableCount(recipe);
        if (craftable <= 0) return false;

        activeRecipe = recipe;
        plannedCrafts = craftable;
        completedCrafts = 0;

        _craftCo = StartCoroutine(CraftLoop(recipe));
        OnBusyChanged?.Invoke(true);
        OnProgress01Changed?.Invoke(0f);
        return true;
    }

    private IEnumerator CraftLoop(CraftingRecipeSO recipe)
    {
        float interval = (recipe.craftIntervalOverride > 0f) ? recipe.craftIntervalOverride : defaultIntervalSeconds;
        var wait = new WaitForSeconds(interval);

        while (true)
        {
            int craftable = GetCraftableCount(recipe);
            if (craftable <= 0) break;

            ConsumeOnce(recipe);

            SpawnOutputs(recipe);

            completedCrafts++;
            float t = plannedCrafts <= 0 ? 1f : Mathf.Clamp01((float)completedCrafts / plannedCrafts);
            OnProgress01Changed?.Invoke(t);

            yield return wait;
        }

        // done
        _craftCo = null;
        activeRecipe = null;
        plannedCrafts = 0;
        completedCrafts = 0;

        OnBusyChanged?.Invoke(false);
        OnProgress01Changed?.Invoke(1f);
        OnStorageChanged?.Invoke();
    }

    private void ConsumeOnce(CraftingRecipeSO recipe)
    {
        foreach (var ing in recipe.inputs)
        {
            RemoveStored(ing.item, ing.amount);
        }

        CleanupStored();
        OnStorageChanged?.Invoke();
    }

    private void SpawnOutputs(CraftingRecipeSO recipe)
    {
        Vector3 start = spawnPoint ? spawnPoint.position : transform.position;

        int count = Mathf.Max(1, recipe.outputAmount);
        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(recipe.outputPrefab, start, Quaternion.identity);

            var mover = go.GetComponent<BottledProductMover>();
            if (mover == null) mover = go.AddComponent<BottledProductMover>();
            mover.travelDuration = travelSeconds;
            mover.arcHeight = arcHeight;
            mover.target = finishedArea;
        }
    }

    // ------------------------- Helpers -------------------------

    private StoredItem FindStored(ItemDefinition item)
    {
        for (int i = 0; i < stored.Count; i++)
            if (stored[i].item == item) return stored[i];
        return null;
    }

    private int CountStored(ItemDefinition item)
    {
        var e = FindStored(item);
        return e == null ? 0 : Mathf.Max(0, e.amount);
    }

    private void RemoveStored(ItemDefinition item, int amount)
    {
        var e = FindStored(item);
        if (e == null) return;
        e.amount = Mathf.Max(0, e.amount - Mathf.Max(0, amount));
    }

    private void CleanupStored()
    {
        for (int i = stored.Count - 1; i >= 0; i--)
            if (stored[i].item == null || stored[i].amount <= 0)
                stored.RemoveAt(i);
    }

    private int CountUniqueTypes()
    {
        int count = 0;
        for (int i = 0; i < stored.Count; i++)
            if (stored[i].item != null && stored[i].amount > 0) count++;
        return count;
    }

    private bool IsKnownIngredient(ItemDefinition item)
    {
        foreach (var r in recipes)
        {
            if (r == null) continue;
            if (r.category != category) continue;
            foreach (var ing in r.inputs)
                if (ing.item == item) return true;
        }
        return false;
    }
}
