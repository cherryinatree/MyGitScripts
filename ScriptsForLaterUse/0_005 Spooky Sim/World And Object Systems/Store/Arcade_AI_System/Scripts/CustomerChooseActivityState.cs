using UnityEngine;

namespace Cherry.ArcadeAI
{
    public class CustomerChooseActivityState : ArcadeAIState
    {
        [Header("Transitions")]
        public ArcadeAIState goToStationState;
        public ArcadeAIState leaveArcadeState;

        [Header("Safety")]
        [Tooltip("Prevents infinite loops if every planned activity is unavailable.")]
        public int maxActivitySearchAttempts = 10;

        public override void Enter()
        {
            base.Enter();

            CustomerBrain customer = brain as CustomerBrain;
            if (customer == null)
            {
                ChangeTo(leaveArcadeState);
                return;
            }

            int attempts = Mathf.Max(1, maxActivitySearchAttempts);

            for (int i = 0; i < attempts; i++)
            {
                CustomerActivityType nextActivity = customer.GetNextActivity();

                if (nextActivity == CustomerActivityType.Leave)
                {
                    ChangeTo(leaveArcadeState);
                    return;
                }

                ArcadeStation station = ArcadeStationRegistry.Instance != null
                    ? ArcadeStationRegistry.Instance.FindBestStationForActivity(nextActivity)
                    : null;

                if (station == null)
                {
                    customer.FailCurrentActivity(GetNoStationComplaint(nextActivity), 15, GetIssueTypeForNoStation(nextActivity));
                    continue;
                }

                customer.currentStation = station;
                customer.currentActivity = nextActivity;
                customer.stationServiceFinished = false;

                ChangeTo(goToStationState);
                return;
            }

            if (customer.reviewMemory != null)
                customer.reviewMemory.AddComplaint("There was not enough to do", 15, ArcadeReviewIssueType.NotEnoughGames);

            ChangeTo(leaveArcadeState);
        }

        private string GetNoStationComplaint(CustomerActivityType activity)
        {
            switch (activity)
            {
                case CustomerActivityType.PlayGame:
                case CustomerActivityType.EatWhilePlaying:
                    return "There were not enough games";

                case CustomerActivityType.GetIceCream:
                    return "No ice cream was available";

                case CustomerActivityType.GetPizza:
                    return "No pizza was available";

                case CustomerActivityType.Eat:
                    return "There was nowhere to sit";

                case CustomerActivityType.BuyItem:
                case CustomerActivityType.Checkout:
                    return "Checkout was not available";

                default:
                    return "There was not enough to do";
            }
        }

        private ArcadeReviewIssueType GetIssueTypeForNoStation(CustomerActivityType activity)
        {
            switch (activity)
            {
                case CustomerActivityType.PlayGame:
                case CustomerActivityType.EatWhilePlaying:
                    return ArcadeReviewIssueType.NotEnoughGames;

                case CustomerActivityType.GetIceCream:
                case CustomerActivityType.GetPizza:
                    return ArcadeReviewIssueType.FoodUnavailable;

                case CustomerActivityType.Eat:
                    return ArcadeReviewIssueType.NoSeatAvailable;

                default:
                    return ArcadeReviewIssueType.NotEnoughGames;
            }
        }
    }
}
