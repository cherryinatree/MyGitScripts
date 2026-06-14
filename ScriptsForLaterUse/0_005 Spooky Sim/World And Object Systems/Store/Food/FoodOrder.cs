using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class FoodOrder
{
    public int orderId = -1;
    public CustomerOrderType orderType;

    [Header("Ice Cream")]
    [Tooltip("Legacy single-flavor field. New cones use requiredIceCreamScoops.")]
    public IceCreamFlavor iceCreamFlavor = IceCreamFlavor.None;

    [Tooltip("Requested cone scoops. Order does not matter when matching.")]
    public List<IceCreamFlavor> requiredIceCreamScoops = new List<IceCreamFlavor>();

    [Header("Pizza")]
    public AmountLevel sauceAmount = AmountLevel.None;
    public AmountLevel cheeseAmount = AmountLevel.None;

    [Header("Shared")]
    public List<FoodIngredient> toppings = new List<FoodIngredient>();

    public bool Matches(FoodItem item, bool requireMatchingOrderId)
    {
        if (item == null) return false;
        if (item.isRuined) return false;

        if (requireMatchingOrderId && item.assignedOrderId != orderId)
            return false;

        switch (orderType)
        {
            case CustomerOrderType.IceCream:
                return MatchesIceCream(item);

            case CustomerOrderType.Pizza:
                return MatchesPizza(item);

            default:
                return false;
        }
    }

    private bool MatchesIceCream(FoodItem item)
    {
        if (item.foodKind != FoodKind.IceCreamCone) return false;
        if (!SameIceCreamScoops(item.iceCreamScoops, GetRequiredIceCreamScoops())) return false;
        return SameToppings(item.toppings, toppings);
    }

    private bool MatchesPizza(FoodItem item)
    {
        if (item.foodKind != FoodKind.CookedPizza) return false;
        if (!item.isCooked) return false;
        if (item.sauceAmount != sauceAmount) return false;
        if (item.cheeseAmount != cheeseAmount) return false;
        return SameToppings(item.toppings, toppings);
    }

    public List<IceCreamFlavor> GetRequiredIceCreamScoops()
    {
        if (requiredIceCreamScoops != null && requiredIceCreamScoops.Count > 0)
            return requiredIceCreamScoops;

        // Backwards compatibility for any older hand-made orders.
        List<IceCreamFlavor> fallback = new List<IceCreamFlavor>();
        if (iceCreamFlavor != IceCreamFlavor.None)
            fallback.Add(iceCreamFlavor);

        return fallback;
    }

    private bool SameIceCreamScoops(List<IceCreamFlavor> actual, List<IceCreamFlavor> required)
    {
        actual ??= new List<IceCreamFlavor>();
        required ??= new List<IceCreamFlavor>();

        Dictionary<IceCreamFlavor, int> actualCounts = CountScoops(actual);
        Dictionary<IceCreamFlavor, int> requiredCounts = CountScoops(required);

        if (actualCounts.Count != requiredCounts.Count) return false;

        foreach (var pair in requiredCounts)
        {
            if (!actualCounts.TryGetValue(pair.Key, out int actualCount))
                return false;

            if (actualCount != pair.Value)
                return false;
        }

        return true;
    }

    private Dictionary<IceCreamFlavor, int> CountScoops(List<IceCreamFlavor> source)
    {
        Dictionary<IceCreamFlavor, int> counts = new Dictionary<IceCreamFlavor, int>();

        foreach (IceCreamFlavor flavor in source)
        {
            if (flavor == IceCreamFlavor.None) continue;

            if (!counts.ContainsKey(flavor))
                counts[flavor] = 0;

            counts[flavor]++;
        }

        return counts;
    }

    private bool SameToppings(List<FoodIngredient> actual, List<FoodIngredient> required)
    {
        actual ??= new List<FoodIngredient>();
        required ??= new List<FoodIngredient>();

        Dictionary<FoodIngredient, int> actualCounts = CountToppings(actual);
        Dictionary<FoodIngredient, int> requiredCounts = CountToppings(required);

        if (actualCounts.Count != requiredCounts.Count) return false;

        foreach (var pair in requiredCounts)
        {
            if (!actualCounts.TryGetValue(pair.Key, out int actualCount))
                return false;

            if (actualCount != pair.Value)
                return false;
        }

        return true;
    }

    private Dictionary<FoodIngredient, int> CountToppings(List<FoodIngredient> source)
    {
        Dictionary<FoodIngredient, int> counts = new Dictionary<FoodIngredient, int>();

        foreach (FoodIngredient topping in source)
        {
            if (topping == FoodIngredient.None) continue;

            if (!counts.ContainsKey(topping))
                counts[topping] = 0;

            counts[topping]++;
        }

        return counts;
    }

    public string GetReadableText()
    {
        StringBuilder builder = new StringBuilder();

        if (orderType == CustomerOrderType.IceCream)
        {
            builder.Append(IceCreamScoopText());

            if (toppings.Count > 0)
            {
                builder.Append(" with ");
                builder.Append(ToppingText());
            }

            return builder.ToString();
        }

        builder.Append("Pizza with ");
        builder.Append(sauceAmount.ToFriendlyText());
        builder.Append(" sauce, ");
        builder.Append(cheeseAmount.ToFriendlyText());
        builder.Append(" cheese");

        if (toppings.Count > 0)
        {
            builder.Append(", and ");
            builder.Append(ToppingText());
        }

        return builder.ToString();
    }

    private string IceCreamScoopText()
    {
        Dictionary<IceCreamFlavor, int> counts = CountScoops(GetRequiredIceCreamScoops());
        List<string> parts = new List<string>();

        AddScoopTextPart(parts, counts, IceCreamFlavor.Vanilla);
        AddScoopTextPart(parts, counts, IceCreamFlavor.Chocolate);
        AddScoopTextPart(parts, counts, IceCreamFlavor.Strawberry);

        if (parts.Count == 0)
            return "ice cream cone";

        return JoinReadable(parts) + " on a cone";
    }

    private void AddScoopTextPart(List<string> parts, Dictionary<IceCreamFlavor, int> counts, IceCreamFlavor flavor)
    {
        if (!counts.TryGetValue(flavor, out int count) || count <= 0)
            return;

        string flavorName = SplitCamelCase(flavor.ToString()).ToLower();
        string scoopWord = count == 1 ? "scoop" : "scoops";
        parts.Add($"{count} {scoopWord} of {flavorName}");
    }

    private string ToppingText()
    {
        if (toppings == null || toppings.Count == 0)
            return "no toppings";

        List<string> names = new List<string>();
        foreach (FoodIngredient topping in toppings)
        {
            if (topping == FoodIngredient.None) continue;
            names.Add(SplitCamelCase(topping.ToString()).ToLower());
        }

        if (names.Count == 0)
            return "no toppings";

        return JoinReadable(names);
    }

    private string JoinReadable(List<string> parts)
    {
        if (parts == null || parts.Count == 0) return "";
        if (parts.Count == 1) return parts[0];
        if (parts.Count == 2) return parts[0] + " and " + parts[1];

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < parts.Count; i++)
        {
            if (i > 0)
                builder.Append(i == parts.Count - 1 ? ", and " : ", ");

            builder.Append(parts[i]);
        }

        return builder.ToString();
    }

    private string SplitCamelCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        StringBuilder builder = new StringBuilder(input.Length + 8);
        builder.Append(input[0]);

        for (int i = 1; i < input.Length; i++)
        {
            char current = input[i];
            if (char.IsUpper(current))
                builder.Append(' ');

            builder.Append(current);
        }

        return builder.ToString();
    }
}
