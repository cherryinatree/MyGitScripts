using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages one food station's line.
/// For pizza, call SendCurrentCustomerToPizzaWaiting() when the raw pizza is placed into the oven.
/// For ice cream, the current customer stays at the counter until they receive the food.
/// </summary>
public class FoodStationLineController : MonoBehaviour
{
    [Header("Station Type")]
    public CustomerOrderType stationOrderType = CustomerOrderType.IceCream;
    public bool requireMatchingOrderIdForPizza = true;

    [Header("Spots")]
    public Transform counterSpot;
    public List<Transform> queueSpots = new List<Transform>();
    public List<Transform> pizzaWaitingSpots = new List<Transform>();

    [Header("Optional Creator To Keep Synced")]
    [Tooltip("Assign your cone creator or pizza base creator here if it needs the current customer/order.")]
    public FoodItemCreator itemCreatorToSync;
    public PizzaBaseCreator pizzaBaseCreatorToSync;

    [Header("Runtime")]
    [SerializeField] private FoodCustomerStationAgent currentCustomer;
    [SerializeField] private List<FoodCustomerStationAgent> queuedCustomers = new List<FoodCustomerStationAgent>();
    [SerializeField] private List<FoodCustomerStationAgent> pizzaWaitingCustomers = new List<FoodCustomerStationAgent>();
    [SerializeField] private List<FoodCustomerStationAgent> pizzaWaitingSpotOccupants = new List<FoodCustomerStationAgent>();

    [Header("Events")]
    public UnityEvent onCustomerBecameCurrent;
    public UnityEvent onCustomerQueued;
    public UnityEvent onCurrentCustomerSentToWaiting;
    public UnityEvent onLineBecameEmpty;

    public FoodCustomerStationAgent CurrentCustomerAgent => currentCustomer;
    public FoodCustomerOrder CurrentCustomerOrder => currentCustomer != null ? currentCustomer.customerOrder : null;

    private void Awake()
    {
        EnsureWaitingSpotListSize();
        SyncCreators();
    }

    public void EnqueueCustomer(FoodCustomerStationAgent agent)
    {
        if (agent == null) return;
        if (agent.customerOrder == null) return;
        if (agent.station != this) agent.station = this;

        agent.customerOrder.orderType = stationOrderType;
        agent.customerOrder.requireMatchingOrderId = stationOrderType == CustomerOrderType.Pizza && requireMatchingOrderIdForPizza;

        if (currentCustomer == null)
        {
            MakeCurrent(agent);
            return;
        }

        if (!queuedCustomers.Contains(agent) && currentCustomer != agent && !pizzaWaitingCustomers.Contains(agent))
        {
            queuedCustomers.Add(agent);
            PositionQueuedCustomers();
            onCustomerQueued?.Invoke();
        }
    }

    public void SendCurrentCustomerToPizzaWaiting()
    {
        if (stationOrderType != CustomerOrderType.Pizza) return;
        if (currentCustomer == null) return;

        FoodCustomerStationAgent customerToWait = currentCustomer;
        currentCustomer = null;

        Transform waitingSpot = ClaimPizzaWaitingSpot(customerToWait);
        if (waitingSpot != null)
            customerToWait.MoveToPizzaWaitingSpot(waitingSpot);

        if (!pizzaWaitingCustomers.Contains(customerToWait))
            pizzaWaitingCustomers.Add(customerToWait);

        SyncCreators();
        onCurrentCustomerSentToWaiting?.Invoke();
        TryServeNextCustomer();
    }

    public void NotifyCustomerFinished(FoodCustomerStationAgent agent)
    {
        if (agent == null) return;

        if (currentCustomer == agent)
            currentCustomer = null;

        queuedCustomers.Remove(agent);
        pizzaWaitingCustomers.Remove(agent);
        ReleasePizzaWaitingSpot(agent);

        PositionQueuedCustomers();
        SyncCreators();
        TryServeNextCustomer();
    }

    public void TryServeNextCustomer()
    {
        if (currentCustomer != null) return;

        if (queuedCustomers.Count == 0)
        {
            SyncCreators();
            onLineBecameEmpty?.Invoke();
            return;
        }

        FoodCustomerStationAgent next = queuedCustomers[0];
        queuedCustomers.RemoveAt(0);
        MakeCurrent(next);
        PositionQueuedCustomers();
    }

    private void MakeCurrent(FoodCustomerStationAgent agent)
    {
        currentCustomer = agent;

        if (agent.customerOrder != null)
        {
            agent.customerOrder.orderType = stationOrderType;
            agent.customerOrder.requireMatchingOrderId = stationOrderType == CustomerOrderType.Pizza && requireMatchingOrderIdForPizza;

            FoodMoneyPayout payout = agent.GetComponent<FoodMoneyPayout>();
            if (payout != null)
                payout.ResetPaymentLock();

            if (agent.generateOrderWhenServed)
                agent.customerOrder.GenerateNewOrder();
        }

        if (counterSpot != null)
            agent.MoveToCounter(counterSpot);

        SyncCreators();
        onCustomerBecameCurrent?.Invoke();
    }

    private void PositionQueuedCustomers()
    {
        for (int i = 0; i < queuedCustomers.Count; i++)
        {
            if (queuedCustomers[i] == null) continue;

            Transform spot = null;
            if (queueSpots != null && queueSpots.Count > 0)
                spot = queueSpots[Mathf.Min(i, queueSpots.Count - 1)];

            if (spot != null)
                queuedCustomers[i].MoveToQueueSpot(spot);
        }
    }

    private Transform ClaimPizzaWaitingSpot(FoodCustomerStationAgent agent)
    {
        EnsureWaitingSpotListSize();

        for (int i = 0; i < pizzaWaitingSpotOccupants.Count; i++)
        {
            if (pizzaWaitingSpotOccupants[i] == null)
            {
                pizzaWaitingSpotOccupants[i] = agent;
                return pizzaWaitingSpots[i];
            }
        }

        if (pizzaWaitingSpots != null && pizzaWaitingSpots.Count > 0)
            return pizzaWaitingSpots[pizzaWaitingSpots.Count - 1];

        return null;
    }

    private void ReleasePizzaWaitingSpot(FoodCustomerStationAgent agent)
    {
        EnsureWaitingSpotListSize();

        for (int i = 0; i < pizzaWaitingSpotOccupants.Count; i++)
        {
            if (pizzaWaitingSpotOccupants[i] == agent)
                pizzaWaitingSpotOccupants[i] = null;
        }
    }

    private void EnsureWaitingSpotListSize()
    {
        if (pizzaWaitingSpots == null)
            pizzaWaitingSpots = new List<Transform>();

        if (pizzaWaitingSpotOccupants == null)
            pizzaWaitingSpotOccupants = new List<FoodCustomerStationAgent>();

        while (pizzaWaitingSpotOccupants.Count < pizzaWaitingSpots.Count)
            pizzaWaitingSpotOccupants.Add(null);

        while (pizzaWaitingSpotOccupants.Count > pizzaWaitingSpots.Count)
            pizzaWaitingSpotOccupants.RemoveAt(pizzaWaitingSpotOccupants.Count - 1);
    }

    private void SyncCreators()
    {
        FoodCustomerOrder order = CurrentCustomerOrder;

        if (itemCreatorToSync != null)
            itemCreatorToSync.currentCustomer = order;

        if (pizzaBaseCreatorToSync != null)
            pizzaBaseCreatorToSync.currentCustomer = order;
    }
}
