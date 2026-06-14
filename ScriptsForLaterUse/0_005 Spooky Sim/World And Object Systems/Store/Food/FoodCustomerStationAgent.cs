using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Put this on a customer that participates in a food station line.
/// It bridges FoodCustomerOrder, simple movement, station queueing, and post-order behavior.
/// </summary>
[RequireComponent(typeof(FoodCustomerOrder))]
public class FoodCustomerStationAgent : MonoBehaviour
{
    [Header("References")]
    public FoodCustomerOrder customerOrder;
    public FoodCustomerMover mover;
    public FoodStationLineController station;

    [Header("Registration")]
    public bool registerOnStart = true;
    public bool generateOrderWhenServed = true;

    [Header("After Order")]
    public bool wanderAfterSuccessfulOrder = true;
    public Transform exitPoint;
    public List<Transform> eatingWanderPoints = new List<Transform>();
    public float eatingWanderDelay = 1.25f;
    public int eatingWanderLoops = 4;

    [Header("Events")]
    public UnityEvent onSentToCounter;
    public UnityEvent onSentToQueueSpot;
    public UnityEvent onSentToPizzaWaitingSpot;
    public UnityEvent onLeavingStation;
    public UnityEvent onStartedEatingWander;

    private Coroutine eatingRoutine;

    private void Reset()
    {
        customerOrder = GetComponent<FoodCustomerOrder>();
        mover = GetComponent<FoodCustomerMover>();
    }

    private void Awake()
    {
        if (customerOrder == null) customerOrder = GetComponent<FoodCustomerOrder>();
        if (mover == null) mover = GetComponent<FoodCustomerMover>();
    }

    private void Start()
    {
        if (registerOnStart && station != null)
            station.EnqueueCustomer(this);
    }

    public void RegisterWithStation(FoodStationLineController targetStation)
    {
        station = targetStation;
        if (station != null)
            station.EnqueueCustomer(this);
    }

    public void MoveToCounter(Transform counterPoint)
    {
        MoveTo(counterPoint);
        onSentToCounter?.Invoke();
    }

    public void MoveToQueueSpot(Transform queuePoint)
    {
        MoveTo(queuePoint);
        onSentToQueueSpot?.Invoke();
    }

    public void MoveToPizzaWaitingSpot(Transform waitingPoint)
    {
        MoveTo(waitingPoint);
        onSentToPizzaWaitingSpot?.Invoke();
    }

    public void MoveTo(Transform point)
    {
        if (mover != null)
            mover.MoveTo(point);
        else if (point != null)
            transform.position = point.position;
    }

    /// <summary>
    /// Wire this to FoodCustomerOrder.onOrderAccepted in the inspector.
    /// </summary>
    public void HandleOrderAccepted()
    {
        if (station != null)
            station.NotifyCustomerFinished(this);

        if (wanderAfterSuccessfulOrder && eatingWanderPoints != null && eatingWanderPoints.Count > 0)
            StartEatingWander();
        else
            LeaveStation();
    }

    /// <summary>
    /// Wire this to FoodCustomerOrder.onOrderFailed in the inspector.
    /// </summary>
    public void HandleOrderFailed()
    {
        if (station != null)
            station.NotifyCustomerFinished(this);

        LeaveStation();
    }

    public void LeaveStation()
    {
        if (eatingRoutine != null)
        {
            StopCoroutine(eatingRoutine);
            eatingRoutine = null;
        }

        if (exitPoint != null)
            MoveTo(exitPoint);

        onLeavingStation?.Invoke();
    }

    public void StartEatingWander()
    {
        if (eatingRoutine != null)
            StopCoroutine(eatingRoutine);

        eatingRoutine = StartCoroutine(EatingWanderRoutine());
        onStartedEatingWander?.Invoke();
    }

    private IEnumerator EatingWanderRoutine()
    {
        int loops = Mathf.Max(1, eatingWanderLoops);

        for (int i = 0; i < loops; i++)
        {
            Transform point = eatingWanderPoints[Random.Range(0, eatingWanderPoints.Count)];
            MoveTo(point);
            yield return new WaitForSeconds(eatingWanderDelay);
        }

        LeaveStation();
        eatingRoutine = null;
    }
}
