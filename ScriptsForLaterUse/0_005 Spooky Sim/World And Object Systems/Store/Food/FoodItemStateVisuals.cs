using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FoodToppingVisual
{
    public FoodIngredient topping;
    public GameObject visual;
}

[Serializable]
public class IceCreamScoopVisualSlot
{
    [Tooltip("The scoop object to show/hide for this scoop position.")]
    public GameObject scoopObject;

    [Tooltip("Renderer on the scoop object. If blank, the script will try GetComponentInChildren<Renderer>().")]
    public Renderer scoopRenderer;
}

/// <summary>
/// Optional helper for turning child visuals on/off based on FoodItem state.
/// Put this on cone/pizza prefabs if you want scoops, sauce, cheese, toppings, cooked color, etc. to appear automatically.
/// </summary>
[RequireComponent(typeof(FoodItem))]
public class FoodItemStateVisuals : MonoBehaviour
{
    [Header("Food Item")]
    public FoodItem foodItem;
    public bool refreshEveryFrame = true;

    [Header("Ice Cream Multi-Scoop Visuals")]
    [Tooltip("Assign your 3 separate scoop models here in visual order: bottom, middle, top.")]
    public List<IceCreamScoopVisualSlot> scoopSlots = new List<IceCreamScoopVisualSlot>();

    public Material chocolateMaterial;
    public Material vanillaMaterial;
    public Material strawberryMaterial;

    [Header("Legacy Single Scoop Visuals")]
    public GameObject chocolateScoop;
    public GameObject vanillaScoop;
    public GameObject strawberryScoop;

    [Header("Pizza Sauce Visuals")]
    public GameObject lightSauce;
    public GameObject normalSauce;
    public GameObject heavySauce;

    [Header("Pizza Cheese Visuals")]
    public GameObject lightCheese;
    public GameObject normalCheese;
    public GameObject heavyCheese;

    [Header("Toppings")]
    public List<FoodToppingVisual> toppingVisuals = new List<FoodToppingVisual>();

    [Header("State Visuals")]
    public GameObject rawVisual;
    public GameObject cookedVisual;
    public GameObject ruinedVisual;

    private void Reset()
    {
        foodItem = GetComponent<FoodItem>();
    }

    private void Awake()
    {
        if (foodItem == null)
            foodItem = GetComponent<FoodItem>();
    }

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        if (refreshEveryFrame)
            Refresh();
    }

    public void Refresh()
    {
        if (foodItem == null) return;

        RefreshIceCreamScoops();

        SetActive(lightSauce, foodItem.sauceAmount == AmountLevel.Light);
        SetActive(normalSauce, foodItem.sauceAmount == AmountLevel.Normal);
        SetActive(heavySauce, foodItem.sauceAmount == AmountLevel.Heavy);

        SetActive(lightCheese, foodItem.cheeseAmount == AmountLevel.Light);
        SetActive(normalCheese, foodItem.cheeseAmount == AmountLevel.Normal);
        SetActive(heavyCheese, foodItem.cheeseAmount == AmountLevel.Heavy);

        if (toppingVisuals != null)
        {
            foreach (FoodToppingVisual entry in toppingVisuals)
            {
                if (entry == null) continue;
                SetActive(entry.visual, HasTopping(entry.topping));
            }
        }

        bool isPizza = foodItem.foodKind == FoodKind.RawPizza || foodItem.foodKind == FoodKind.CookedPizza;
        SetActive(rawVisual, isPizza && !foodItem.isCooked && !foodItem.isRuined);
        SetActive(cookedVisual, isPizza && foodItem.isCooked && !foodItem.isRuined);
        SetActive(ruinedVisual, foodItem.isRuined);
    }

    private void RefreshIceCreamScoops()
    {
        if (scoopSlots != null && scoopSlots.Count > 0)
        {
            for (int i = 0; i < scoopSlots.Count; i++)
            {
                IceCreamScoopVisualSlot slot = scoopSlots[i];
                if (slot == null) continue;

                bool hasScoop = foodItem.iceCreamScoops != null && i < foodItem.iceCreamScoops.Count;
                SetActive(slot.scoopObject, hasScoop);

                if (!hasScoop) continue;

                Renderer renderer = slot.scoopRenderer;
                if (renderer == null && slot.scoopObject != null)
                {
                    renderer = slot.scoopObject.GetComponentInChildren<Renderer>();
                    slot.scoopRenderer = renderer;
                }

                if (renderer != null)
                {
                    Material material = GetMaterialForFlavor(foodItem.iceCreamScoops[i]);
                    if (material != null && renderer.sharedMaterial != material)
                        renderer.sharedMaterial = material;
                }
            }

            SetActive(chocolateScoop, false);
            SetActive(vanillaScoop, false);
            SetActive(strawberryScoop, false);
            return;
        }

        // Old one-scoop visual mode. Kept for compatibility with older prefabs.
        SetActive(chocolateScoop, foodItem.iceCreamFlavor == IceCreamFlavor.Chocolate);
        SetActive(vanillaScoop, foodItem.iceCreamFlavor == IceCreamFlavor.Vanilla);
        SetActive(strawberryScoop, foodItem.iceCreamFlavor == IceCreamFlavor.Strawberry);
    }

    private Material GetMaterialForFlavor(IceCreamFlavor flavor)
    {
        return flavor switch
        {
            IceCreamFlavor.Chocolate => chocolateMaterial,
            IceCreamFlavor.Vanilla => vanillaMaterial,
            IceCreamFlavor.Strawberry => strawberryMaterial,
            _ => null
        };
    }

    private bool HasTopping(FoodIngredient topping)
    {
        if (foodItem.toppings == null) return false;

        foreach (FoodIngredient entry in foodItem.toppings)
        {
            if (entry == topping)
                return true;
        }

        return false;
    }

    private void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }
}
