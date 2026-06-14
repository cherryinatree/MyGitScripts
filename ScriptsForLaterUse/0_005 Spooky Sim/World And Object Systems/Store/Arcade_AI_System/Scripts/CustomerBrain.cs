using System.Collections.Generic;
using UnityEngine;

namespace Cherry.ArcadeAI
{
    /// <summary>
    /// Customer-specific data and planning.
    /// The actual behavior still lives in states.
    /// </summary>
    public class CustomerBrain : ArcadeAgentBrain
    {
        [Header("Customer Review")]
        public CustomerReviewMemory reviewMemory;

        [Header("Activity Preferences")]
        [Range(0f, 1f)] public float chanceToBuyItem = 0.25f;
        [Range(0f, 1f)] public float chanceToPlayGame = 0.80f;
        [Range(0f, 1f)] public float chanceToGetIceCream = 0.35f;
        [Range(0f, 1f)] public float chanceToGetPizza = 0.35f;
        [Range(0f, 1f)] public float chanceToEatAfterFood = 0.70f;
        [Range(0f, 1f)] public float chanceToEatWhilePlaying = 0.35f;

        [Header("Visit Length")]
        public int minActivities = 1;
        public int maxActivities = 4;

        [Header("Runtime")]
        public ArcadeStation currentStation;
        public CustomerActivityType currentActivity;
        public bool hasFood;
        public bool hasItemToBuy;
        public bool wasCountedByCapacityManager;
        public bool stationServiceFinished;

        [SerializeField] private List<CustomerActivityType> activityPlan = new List<CustomerActivityType>();
        [SerializeField] private int activityIndex;

        protected override void Awake()
        {
            base.Awake();

            if (reviewMemory == null)
                reviewMemory = GetComponent<CustomerReviewMemory>();

            if (reviewMemory == null)
                reviewMemory = gameObject.AddComponent<CustomerReviewMemory>();
        }

        public void BuildActivityPlanIfNeeded()
        {
            if (activityPlan.Count > 0)
                return;

            int activityCount = Random.Range(Mathf.Max(1, minActivities), Mathf.Max(minActivities, maxActivities) + 1);

            for (int i = 0; i < activityCount; i++)
            {
                CustomerActivityType activity = PickWeightedActivity();

                if (activity != CustomerActivityType.None)
                    activityPlan.Add(activity);
            }

            if (activityPlan.Count == 0)
                activityPlan.Add(CustomerActivityType.PlayGame);

            if (hasItemToBuy || activityPlan.Contains(CustomerActivityType.BuyItem))
            {
                if (!activityPlan.Contains(CustomerActivityType.Checkout))
                    activityPlan.Add(CustomerActivityType.Checkout);
            }
        }

        public CustomerActivityType GetNextActivity()
        {
            BuildActivityPlanIfNeeded();

            if (activityIndex >= activityPlan.Count)
                return CustomerActivityType.Leave;

            currentActivity = activityPlan[activityIndex];
            return currentActivity;
        }

        public void CompleteCurrentActivity()
        {
            activityIndex++;
            currentActivity = CustomerActivityType.None;
            currentStation = null;
            stationServiceFinished = false;
        }

        public void FailCurrentActivity(string complaint, int penalty, ArcadeReviewIssueType issueType)
        {
            if (reviewMemory != null)
                reviewMemory.AddComplaint(complaint, penalty, issueType);

            CompleteCurrentActivity();
        }

        public void NotifyStationServiceFinished(ArcadeStation station)
        {
            if (station == currentStation)
                stationServiceFinished = true;
        }

        public void UpdateQueuedDestination(ArcadeStation station)
        {
            if (station == null || station != currentStation)
                return;

            MoveTo(station.GetQueueWorldPosition(this));
        }

        public void RegisterEnteredIfNeeded()
        {
            if (wasCountedByCapacityManager)
                return;

            if (ArcadeCapacityManager.Instance != null)
            {
                ArcadeCapacityManager.Instance.RegisterCustomerEntered();
                wasCountedByCapacityManager = true;
            }
        }

        public void RegisterLeftIfNeeded()
        {
            if (!wasCountedByCapacityManager)
                return;

            if (ArcadeCapacityManager.Instance != null)
                ArcadeCapacityManager.Instance.RegisterCustomerLeft();

            wasCountedByCapacityManager = false;
        }

        private CustomerActivityType PickWeightedActivity()
        {
            List<CustomerActivityType> options = new List<CustomerActivityType>();
            List<float> weights = new List<float>();

            AddWeightedOption(CustomerActivityType.BuyItem, chanceToBuyItem, options, weights);
            AddWeightedOption(CustomerActivityType.PlayGame, chanceToPlayGame, options, weights);
            AddWeightedOption(CustomerActivityType.GetIceCream, chanceToGetIceCream, options, weights);
            AddWeightedOption(CustomerActivityType.GetPizza, chanceToGetPizza, options, weights);

            if (hasFood)
            {
                AddWeightedOption(CustomerActivityType.Eat, chanceToEatAfterFood, options, weights);
                AddWeightedOption(CustomerActivityType.EatWhilePlaying, chanceToEatWhilePlaying, options, weights);
            }

            if (options.Count == 0)
                return CustomerActivityType.PlayGame;

            float total = 0f;
            for (int i = 0; i < weights.Count; i++)
                total += weights[i];

            float roll = Random.Range(0f, total);
            float running = 0f;

            for (int i = 0; i < options.Count; i++)
            {
                running += weights[i];

                if (roll <= running)
                    return options[i];
            }

            return options[options.Count - 1];
        }

        private void AddWeightedOption(CustomerActivityType activity, float weight, List<CustomerActivityType> options, List<float> weights)
        {
            if (weight <= 0f)
                return;

            options.Add(activity);
            weights.Add(weight);
        }
    }
}
