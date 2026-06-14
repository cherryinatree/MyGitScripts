using UnityEngine;

namespace Cherry.DayAndTime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cherry/Day And Time/Store Report Source")]
    public class StoreReportSource : MonoBehaviour, IStoreReportSource
    {
        [Header("Refs")]
        [SerializeField] private DayTimeSystem dayTime;

        [Header("Today Stats (read-only at runtime)")]
        [SerializeField] private int customersToday;
        [SerializeField] private int transactionsToday;
        [SerializeField] private float revenueToday;
        [SerializeField] private float costToday;

        public int CustomersToday => customersToday;
        public int TransactionsToday => transactionsToday;
        public float RevenueToday => revenueToday;
        public float CostToday => costToday;
        public float ProfitToday => revenueToday - costToday;

        private void Awake()
        {
            if (dayTime == null) dayTime = DayTimeSystem.Instance;
        }

        private void OnEnable()
        {
            if (dayTime == null) dayTime = DayTimeSystem.Instance;
            if (dayTime == null) return;

            dayTime.OnStoreOpened += HandleStoreOpened;
            dayTime.OnNewDayStarted += HandleNewDayStarted;
        }

        private void OnDisable()
        {
            if (dayTime == null) return;

            dayTime.OnStoreOpened -= HandleStoreOpened;
            dayTime.OnNewDayStarted -= HandleNewDayStarted;
        }

        private void HandleStoreOpened(int _day)
        {
            // Fresh day when store opens (safe even if you already reset on new day)
            ResetToday();
        }

        private void HandleNewDayStarted(int _day)
        {
            ResetToday();
        }

        public void ResetToday()
        {
            customersToday = 0;
            transactionsToday = 0;
            revenueToday = 0f;
            costToday = 0f;
        }

        // ---- Call these from your gameplay systems ----

        /// <summary>Call when a customer enters the store (or is counted for the day).</summary>
        public void RegisterCustomer(int count = 1)
        {
            customersToday += Mathf.Max(0, count);
        }

        /// <summary>
        /// Call when a sale happens. Provide revenue and optional cost (COGS) so we can compute profit.
        /// </summary>
        public void RegisterSale(float revenue, float cost = 0f, int transactions = 1)
        {
            transactionsToday += Mathf.Max(0, transactions);
            revenueToday += Mathf.Max(0f, revenue);
            costToday += Mathf.Max(0f, cost);
        }

        // ---- DayTimeSystem will call this when it needs the report ----
        public DayReport BuildDayReport(int dayNumber, int minutesOpen)
        {
            float profit = ProfitToday;

            return new DayReport
            {
                dayNumber = dayNumber,
                minutesOpen = minutesOpen,
                customers = customersToday,
                transactions = transactionsToday,
                revenue = revenueToday,
                profit = profit,
                notes = profit >= 0f ? "Nice work." : "Rough day—consider pricing/costs."
            };
        }
    }
}
