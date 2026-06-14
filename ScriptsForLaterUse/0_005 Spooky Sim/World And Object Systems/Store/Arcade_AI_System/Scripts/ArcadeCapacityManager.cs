using UnityEngine;

namespace Cherry.ArcadeAI
{
    /// <summary>
    /// Determines how many customers can be inside based on arcade size and activities.
    /// Can use manually-entered counts or auto-count registered stations.
    /// </summary>
    public class ArcadeCapacityManager : MonoBehaviour
    {
        public static ArcadeCapacityManager Instance { get; private set; }

        [Header("Arcade Size")]
        public int arcadeSizeLevel = 1;
        public int baseCapacityPerSizeLevel = 5;

        [Header("Use Station Registry Counts")]
        public bool useRegisteredStationCounts = true;

        [Header("Manual Attraction Counts")]
        public int arcadeGameCount;
        public int foodStationCount;
        public int seatingCount;
        public int prizeCounterCount;

        [Header("Capacity Values")]
        public int customersPerGame = 1;
        public int customersPerFoodStation = 2;
        public int customersPerSeat = 1;
        public int customersPerPrizeCounter = 2;

        [Header("Runtime")]
        [SerializeField] private int currentCustomersInside;

        public int CurrentCustomersInside => currentCustomersInside;

        public int MaxCustomersAllowed
        {
            get
            {
                int gameCount = arcadeGameCount;
                int foodCount = foodStationCount;
                int seatCount = seatingCount;
                int prizeCount = prizeCounterCount;

                if (useRegisteredStationCounts && ArcadeStationRegistry.Instance != null)
                {
                    gameCount = ArcadeStationRegistry.Instance.CountStationsOfType(ArcadeStationType.ArcadeGame);
                    foodCount =
                        ArcadeStationRegistry.Instance.CountStationsOfType(ArcadeStationType.IceCream) +
                        ArcadeStationRegistry.Instance.CountStationsOfType(ArcadeStationType.Pizza);

                    seatCount = ArcadeStationRegistry.Instance.CountStationsOfType(ArcadeStationType.Dining);
                    prizeCount = ArcadeStationRegistry.Instance.CountStationsOfType(ArcadeStationType.PrizeCounter);
                }

                int sizeCapacity = Mathf.Max(1, arcadeSizeLevel) * Mathf.Max(1, baseCapacityPerSizeLevel);

                int activityCapacity =
                    gameCount * customersPerGame +
                    foodCount * customersPerFoodStation +
                    seatCount * customersPerSeat +
                    prizeCount * customersPerPrizeCounter;

                return Mathf.Max(1, sizeCapacity + activityCapacity);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public bool CanCustomerEnter()
        {
            return currentCustomersInside < MaxCustomersAllowed;
        }

        public void RegisterCustomerEntered()
        {
            currentCustomersInside = Mathf.Clamp(currentCustomersInside + 1, 0, MaxCustomersAllowed);
        }

        public void RegisterCustomerLeft()
        {
            currentCustomersInside = Mathf.Max(0, currentCustomersInside - 1);
        }

        public void ResetCurrentCustomerCount()
        {
            currentCustomersInside = 0;
        }
    }
}
