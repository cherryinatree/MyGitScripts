using UnityEngine;

namespace Cherry.ArcadeAI
{
    public class CustomerUseStationState : ArcadeAIState
    {
        [Header("Transitions")]
        public ArcadeAIState failedState;

        private bool serviceStarted;

        public override void Enter()
        {
            base.Enter();

            serviceStarted = false;

            CustomerBrain customer = brain as CustomerBrain;
            if (customer == null || customer.currentStation == null)
            {
                ChangeTo(failedState);
                return;
            }

            brain.MoveTo(customer.currentStation.GetServiceWorldPosition());
        }

        public override void Tick()
        {
            CustomerBrain customer = brain as CustomerBrain;

            if (customer == null || customer.currentStation == null)
            {
                ChangeTo(failedState);
                return;
            }

            ArcadeStation station = customer.currentStation;

            if (!serviceStarted)
            {
                brain.MoveTo(station.GetServiceWorldPosition());

                if (!brain.HasReachedDestination())
                    return;

                serviceStarted = station.BeginServiceForCustomer(customer);

                if (!serviceStarted)
                {
                    station.LeaveQueue(customer);
                    customer.FailCurrentActivity("Service was not available", 15, ArcadeReviewIssueType.NoWorkerAvailable);
                    ChangeTo(failedState);
                    return;
                }
            }

            if (customer.stationServiceFinished)
            {
                customer.CompleteCurrentActivity();
                CompleteState();
            }
        }

        public override void Exit()
        {
            if (brain != null)
                brain.StopMoving();

            base.Exit();
        }
    }
}
