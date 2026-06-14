using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class RecipeIngredientRequirement
{
    public string ingredientId;
    public int requiredAmount = 1;
    public int currentAmount = 0;
}

public class FoodRecipeBatchMaker : MonoBehaviour
{
    [Header("Recipe")]
    public string recipeName = "Chocolate Ice Cream Mix";
    public List<RecipeIngredientRequirement> requirements = new List<RecipeIngredientRequirement>();

    [Header("Output")]
    public FoodItem outputPrefab;
    public Transform outputSpawnPoint;
    public bool putOutputInHands = true;

    [Header("Events")]
    public UnityEvent onIngredientAdded;
    public UnityEvent onCrafted;
    public UnityEvent onCraftFailed;

    public bool AddIngredient(string ingredientId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(ingredientId) || amount <= 0) return false;

        RecipeIngredientRequirement requirement = requirements.Find(r => r.ingredientId == ingredientId);
        if (requirement == null) return false;
        if (requirement.currentAmount >= requirement.requiredAmount) return false;

        requirement.currentAmount = Mathf.Min(requirement.requiredAmount, requirement.currentAmount + amount);
        onIngredientAdded?.Invoke();
        return true;
    }

    public bool CanCraft()
    {
        if (outputPrefab == null) return false;

        foreach (RecipeIngredientRequirement requirement in requirements)
        {
            if (requirement.currentAmount < requirement.requiredAmount)
                return false;
        }

        return true;
    }

    public bool TryCraft(PlayerFoodHands hands = null)
    {
        if (!CanCraft())
        {
            onCraftFailed?.Invoke();
            return false;
        }

        if (putOutputInHands && hands != null && !hands.CanHoldFood())
        {
            onCraftFailed?.Invoke();
            return false;
        }

        Transform point = outputSpawnPoint != null ? outputSpawnPoint : transform;
        FoodItem output = Instantiate(outputPrefab, point.position, point.rotation);

        if (putOutputInHands && hands != null)
            hands.TryHoldExistingFood(output);

        ResetRecipeProgress();
        onCrafted?.Invoke();
        return true;
    }

    public void ResetRecipeProgress()
    {
        foreach (RecipeIngredientRequirement requirement in requirements)
            requirement.currentAmount = 0;
    }
}
