using UnityEngine;

namespace Cherry.ArcadeAI
{
    public class CustomerGoToStationState : ArcadeAIState
    {
        [Header("Transitions")]
        public ArcadeAIState failedState;

        public override void Enter()
        {
            base.Enter();

            CustomerBrain customer = brain as CustomerBrain;
            if (customer == null || customer.currentStation == null)
            {
                ChangeTo(failedState);
                return;
            }

            bool joined = customer.currentStation.JoinQueue(customer);

            if (!joined)
            {
                customer.FailCurrentActivity("The arcade was too crowded", 15, ArcadeReviewIssueType.TooCrowded);
                ChangeTo(failedState);
                return;
            }

            brain.MoveTo(customer.currentStation.GetQueueWorldPosition(customer));
        }

        public override void Tick()
        {
            CustomerBrain customer = brain as CustomerBrain;

            if (customer == null || customer.currentStation == null)
            {
                ChangeTo(failedState);
                return;
            }

            brain.MoveTo(customer.currentStation.GetQueueWorldPosition(customer));

            if (brain.HasReachedDestination())
                CompleteState();
        }
    }
}
