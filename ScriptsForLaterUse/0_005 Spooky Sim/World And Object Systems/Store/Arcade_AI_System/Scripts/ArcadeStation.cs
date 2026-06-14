using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Cherry.ArcadeAI
{
    /// <summary>
    /// Generic station for checkout counters, ice cream, pizza, arcade machines, dining spots, etc.
    /// Customers queue here. Workers can be assigned here.
    /// </summary>
    public class ArcadeStation : MonoBehaviour
    {
        [Header("Station")]
        public string stationName = "Arcade Station";
        public ArcadeStationType stationType = ArcadeStationType.General;

        [Tooltip("If true, customers can use this station without a worker. Good for arcade cabinets and dining tables.")]
        public bool allowUseWithoutWorker = false;

        [Tooltip("If true, only one customer can use the station at a time.")]
        public bool oneCustomerAtATime = true;

        [Header("Points")]
        public Transform servicePoint;
        public Transform workerStandPoint;
        public List<Transform> queuePoints = new List<Transform>();

        [Header("Queue")]
        public int maxQueueSize = 5;

        [Header("Service")]
        public float serviceTime = 5f;

        [Header("Condition")]
        public bool isBroken;
        public bool isDirty;

        [Header("Runtime")]
        [SerializeField] private List<CustomerBrain> queue = new List<CustomerBrain>();
        [SerializeField] private CustomerBrain activeCustomer;
        [SerializeField] private WorkerBrain assignedWorker;
        [SerializeField] private bool serviceInProgress;
        [SerializeField] private float serviceTimer;

        [Header("Events")]
        public UnityEvent onCustomerJoinedQueue;
        public UnityEvent onCustomerLeftQueue;
        public UnityEvent onServiceStarted;
        public UnityEvent onServiceCompleted;
        public UnityEvent onWorkerAssigned;
        public UnityEvent onWorkerRemoved;

        public int QueueCount => queue.Count;
        public bool ServiceInProgress => serviceInProgress;
        public CustomerBrain ActiveCustomer => activeCustomer;
        public WorkerBrain AssignedWorker => assignedWorker;

        private void OnEnable()
        {
            if (ArcadeStationRegistry.Instance != null)
                ArcadeStationRegistry.Instance.Register(this);
        }

        private void Start()
        {
            if (ArcadeStationRegistry.Instance != null)
                ArcadeStationRegistry.Instance.Register(this);
        }

        private void OnDisable()
        {
            if (ArcadeStationRegistry.Instance != null)
                ArcadeStationRegistry.Instance.Unregister(this);
        }

        private void Update()
        {
            CleanNullCustomers();

            if (!serviceInProgress)
                return;

            serviceTimer -= Time.deltaTime;

            if (serviceTimer <= 0f)
                CompleteActiveService();
        }

        public bool CanAcceptCustomer()
        {
            if (isBroken)
                return false;

            if (maxQueueSize > 0 && queue.Count >= maxQueueSize)
                return false;

            return true;
        }

        public bool JoinQueue(CustomerBrain customer)
        {
            if (customer == null)
                return false;

            if (queue.Contains(customer))
                return true;

            if (!CanAcceptCustomer())
                return false;

            queue.Add(customer);
            customer.currentStation = this;
            RefreshQueueDestinations();

            onCustomerJoinedQueue?.Invoke();
            return true;
        }

        public void LeaveQueue(CustomerBrain customer)
        {
            if (customer == null)
                return;

            if (queue.Remove(customer))
            {
                if (activeCustomer == customer)
                    CancelActiveService();

                RefreshQueueDestinations();
                onCustomerLeftQueue?.Invoke();
            }
        }

        public bool IsFirstInQueue(CustomerBrain customer)
        {
            CleanNullCustomers();

            if (customer == null || queue.Count == 0)
                return false;

            return queue[0] == customer;
        }

        public Vector3 GetQueueWorldPosition(CustomerBrain customer)
        {
            int index = queue.IndexOf(customer);

            if (index >= 0 && index < queuePoints.Count && queuePoints[index] != null)
                return queuePoints[index].position;

            if (servicePoint != null)
                return servicePoint.position - transform.forward * Mathf.Max(1, index + 1);

            return transform.position - transform.forward * Mathf.Max(1, index + 1);
        }

        public Vector3 GetServiceWorldPosition()
        {
            return servicePoint != null ? servicePoint.position : transform.position;
        }

        public Vector3 GetWorkerStandWorldPosition()
        {
            if (workerStandPoint != null)
                return workerStandPoint.position;

            if (servicePoint != null)
                return servicePoint.position + transform.right;

            return transform.position + transform.right;
        }

        public bool CanServeCustomer(CustomerBrain customer)
        {
            if (customer == null)
                return false;

            if (isBroken)
                return false;

            if (!IsFirstInQueue(customer))
                return false;

            if (!allowUseWithoutWorker && assignedWorker == null)
                return false;

            if (oneCustomerAtATime && serviceInProgress && activeCustomer != customer)
                return false;

            return true;
        }

        public bool BeginServiceForCustomer(CustomerBrain customer)
        {
            if (!CanServeCustomer(customer))
                return false;

            if (serviceInProgress && activeCustomer == customer)
                return true;

            activeCustomer = customer;
            serviceInProgress = true;
            serviceTimer = Mathf.Max(0.05f, serviceTime);

            if (assignedWorker != null)
                assignedWorker.SetBusyWithCustomer(customer, this);

            onServiceStarted?.Invoke();
            return true;
        }

        public void AssignWorker(WorkerBrain worker)
        {
            if (assignedWorker == worker)
                return;

            if (assignedWorker != null)
                assignedWorker.assignedStation = null;

            assignedWorker = worker;

            if (assignedWorker != null)
                assignedWorker.assignedStation = this;

            onWorkerAssigned?.Invoke();
        }

        public void RemoveWorker(WorkerBrain worker)
        {
            if (assignedWorker != worker)
                return;

            assignedWorker = null;
            onWorkerRemoved?.Invoke();
        }

        public void MarkBroken(bool broken)
        {
            isBroken = broken;
        }

        public void MarkDirty(bool dirty)
        {
            isDirty = dirty;
        }

        private void CompleteActiveService()
        {
            if (activeCustomer != null)
            {
                RecordVisitOnCustomer(activeCustomer);
                activeCustomer.NotifyStationServiceFinished(this);
                queue.Remove(activeCustomer);
            }

            if (assignedWorker != null)
                assignedWorker.ClearBusyWithCustomer(activeCustomer, this);

            activeCustomer = null;
            serviceInProgress = false;
            serviceTimer = 0f;

            RefreshQueueDestinations();
            onServiceCompleted?.Invoke();
        }

        private void CancelActiveService()
        {
            if (assignedWorker != null)
                assignedWorker.ClearBusyWithCustomer(activeCustomer, this);

            activeCustomer = null;
            serviceInProgress = false;
            serviceTimer = 0f;
        }

        private void RecordVisitOnCustomer(CustomerBrain customer)
        {
            if (customer == null || customer.reviewMemory == null)
                return;

            if (isDirty)
                customer.reviewMemory.AddComplaint("The arcade was dirty", 15, ArcadeReviewIssueType.StoreDirty);

            switch (stationType)
            {
                case ArcadeStationType.ArcadeGame:
                    customer.reviewMemory.AddPositive("The games were fun");
                    break;

                case ArcadeStationType.IceCream:
                    customer.reviewMemory.AddPositive("The ice cream was good");
                    customer.hasFood = true;
                    break;

                case ArcadeStationType.Pizza:
                    customer.reviewMemory.AddPositive("The pizza was good");
                    customer.hasFood = true;
                    break;

                case ArcadeStationType.Dining:
                    customer.reviewMemory.AddPositive("There was a place to sit and eat");
                    customer.hasFood = false;
                    break;

                case ArcadeStationType.Checkout:
                    customer.reviewMemory.AddPositive("Checkout was easy");
                    break;
            }
        }

        private void RefreshQueueDestinations()
        {
            CleanNullCustomers();

            for (int i = 0; i < queue.Count; i++)
            {
                if (queue[i] != null)
                    queue[i].UpdateQueuedDestination(this);
            }
        }

        private void CleanNullCustomers()
        {
            for (int i = queue.Count - 1; i >= 0; i--)
            {
                if (queue[i] == null)
                    queue.RemoveAt(i);
            }
        }
    }
}
