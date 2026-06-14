using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class FoodCustomerOrder : MonoBehaviour
{
    private static int nextOrderId = 1;

    [Header("Order Type")]
    public CustomerOrderType orderType = CustomerOrderType.IceCream;
    public bool generateOrderOnStart = true;
    public bool requireMatchingOrderId;

    [Header("Ice Cream Order Options")]
    [Range(1, 3)] public int minIceCreamScoops = 1;
    [Range(1, 3)] public int maxIceCreamScoops = 3;

    public List<FoodIngredient> possibleIceCreamToppings = new List<FoodIngredient>
    {
        FoodIngredient.Sprinkles,
        FoodIngredient.CookieCrumbs,
        FoodIngredient.ChocolateSyrup
    };

    [Header("Pizza Order Options")]
    public List<FoodIngredient> possiblePizzaToppings = new List<FoodIngredient>
    {
        FoodIngredient.Pepperoni,
        FoodIngredient.Peppers,
        FoodIngredient.Olives
    };

    [Header("Topping Order Options")]
    [Range(0, 3)] public int minToppings = 0;
    [Range(0, 5)] public int maxToppings = 2;

    [Header("UI")]
    public TextMeshProUGUI orderText;
    public string iceCreamPrefix = "I want ";
    public string pizzaPrefix = "I want ";

    [Header("Payment")]
    public int basePayment = 5;
    public int scoopPaymentBonus = 1;
    public int toppingPaymentBonus = 1;

    [Header("Events")]
    public UnityEvent onOrderAccepted;
    public UnityEvent onOrderRejected;
    public UnityEvent onOrderFailed;

    public FoodOrder CurrentOrder { get; private set; }
    public int CurrentOrderId => CurrentOrder != null ? CurrentOrder.orderId : -1;
    public bool IsWaitingForFood { get; private set; }
    public bool IsCompleted { get; private set; }

    private void Start()
    {
        if (generateOrderOnStart)
            GenerateNewOrder();
    }

    public void GenerateNewOrder()
    {
        CurrentOrder = new FoodOrder
        {
            orderId = nextOrderId++,
            orderType = orderType
        };

        if (orderType == CustomerOrderType.IceCream)
            FillIceCreamOrder(CurrentOrder);
        else
            FillPizzaOrder(CurrentOrder);

        IsWaitingForFood = true;
        IsCompleted = false;
        RefreshOrderText();
    }

    public bool TryReceiveFood(FoodItem foodItem)
    {
        if (!IsWaitingForFood || IsCompleted || CurrentOrder == null)
            return false;

        bool correct = CurrentOrder.Matches(foodItem, requireMatchingOrderId);

        if (correct)
        {
            IsWaitingForFood = false;
            IsCompleted = true;
            onOrderAccepted?.Invoke();
            return true;
        }

        onOrderRejected?.Invoke();
        return false;
    }

    public void MarkOrderFailed()
    {
        if (IsCompleted) return;

        IsWaitingForFood = false;
        IsCompleted = true;
        onOrderFailed?.Invoke();
    }

    public int GetPaymentAmount()
    {
        if (CurrentOrder == null) return basePayment;

        int toppingCount = CurrentOrder.toppings != null ? CurrentOrder.toppings.Count : 0;
        int scoopCount = 0;

        if (CurrentOrder.orderType == CustomerOrderType.IceCream)
            scoopCount = CurrentOrder.GetRequiredIceCreamScoops().Count;

        return basePayment + scoopCount * scoopPaymentBonus + toppingCount * toppingPaymentBonus;
    }

    public void RefreshOrderText()
    {
        if (orderText == null) return;

        if (CurrentOrder == null)
        {
            orderText.text = "";
            return;
        }

        string prefix = orderType == CustomerOrderType.IceCream ? iceCreamPrefix : pizzaPrefix;
        orderText.text = prefix + CurrentOrder.GetReadableText();
    }

    private void FillIceCreamOrder(FoodOrder order)
    {
        order.requiredIceCreamScoops = PickRandomScoops();
        order.iceCreamFlavor = order.requiredIceCreamScoops.Count > 0
            ? order.requiredIceCreamScoops[0]
            : IceCreamFlavor.None;

        order.toppings = PickRandomToppings(possibleIceCreamToppings);
    }

    private void FillPizzaOrder(FoodOrder order)
    {
        order.sauceAmount = (AmountLevel)Random.Range(0, 4);
        order.cheeseAmount = (AmountLevel)Random.Range(0, 4);
        order.toppings = PickRandomToppings(possiblePizzaToppings);
    }

    private List<IceCreamFlavor> PickRandomScoops()
    {
        List<IceCreamFlavor> result = new List<IceCreamFlavor>();

        int min = Mathf.Clamp(minIceCreamScoops, 1, 3);
        int max = Mathf.Clamp(maxIceCreamScoops, min, 3);
        int count = Random.Range(min, max + 1);

        for (int i = 0; i < count; i++)
            result.Add((IceCreamFlavor)Random.Range(1, 4));

        return result;
    }

    private List<FoodIngredient> PickRandomToppings(List<FoodIngredient> source)
    {
        List<FoodIngredient> result = new List<FoodIngredient>();
        if (source == null || source.Count == 0) return result;

        int count = Random.Range(minToppings, maxToppings + 1);
        count = Mathf.Clamp(count, 0, source.Count);

        List<FoodIngredient> pool = new List<FoodIngredient>(source);

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }
}
