using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class FoodMoneyEvent : UnityEvent<int> { }

/// <summary>
/// Pays money when a FoodCustomerOrder is accepted.
/// Wire FoodCustomerOrder.onOrderAccepted to PayCustomer() in the inspector.
/// Then wire onMoneyPaid to your money/store report script.
/// </summary>
public class FoodMoneyPayout : MonoBehaviour
{
    [Header("Customer")]
    public FoodCustomerOrder customer;
    public bool payOnlyOnce = true;

    [Header("Debug")]
    public int totalPaidByThisPayout;
    [SerializeField] private bool hasPaid;

    [Header("Events")]
    public FoodMoneyEvent onMoneyPaid;

    private void Reset()
    {
        customer = GetComponent<FoodCustomerOrder>();
    }

    private void Awake()
    {
        if (customer == null)
            customer = GetComponent<FoodCustomerOrder>();
    }

    public void PayCustomer()
    {
        if (customer == null) return;
        if (payOnlyOnce && hasPaid) return;

        int amount = customer.GetPaymentAmount();
        hasPaid = true;
        totalPaidByThisPayout += amount;
        onMoneyPaid?.Invoke(amount);
    }

    public void ResetPaymentLock()
    {
        hasPaid = false;
    }
}
