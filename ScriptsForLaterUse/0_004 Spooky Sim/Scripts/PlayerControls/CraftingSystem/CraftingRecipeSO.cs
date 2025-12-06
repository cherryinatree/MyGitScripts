using System;
using System.Collections.Generic;
using UnityEngine;
using Cherry.Inventory;

[CreateAssetMenu(menuName = "Stations/Crafting Recipe", fileName = "Recipe_")]
public class CraftingRecipeSO : ScriptableObject
{
    [Header("Station Type")]
    public CraftingCategory category = CraftingCategory.Bottling;

    [Header("UI")]
    public string displayName = "New Recipe";
    public Sprite outputIcon;

    [Header("Inputs (order doesn't matter)")]
    public List<Ingredient> inputs = new();

    [Header("Output")]
    public GameObject outputPrefab;
    [Min(1)] public int outputAmount = 1;

    [Header("Timing")]
    [Tooltip("Seconds per craft cycle (one 'recipe craft'). If 0, station uses its default interval.")]
    [Min(0f)] public float craftIntervalOverride = 0f;

    [Serializable]
    public struct Ingredient
    {
        public ItemDefinition item;
        [Min(1)] public int amount;
    }
}
