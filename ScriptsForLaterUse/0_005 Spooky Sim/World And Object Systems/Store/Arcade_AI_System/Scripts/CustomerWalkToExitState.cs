using UnityEngine;

namespace Cherry.ArcadeAI
{
    public class CustomerWalkToExitState : ArcadeAIState
    {
        [Header("Exit")]
        public Transform exitPoint;

        public override void Enter()
        {
            base.Enter();

            CustomerBrain customer = brain as CustomerBrain;
            if (customer != null && customer.currentStation != null)
                customer.currentStation.LeaveQueue(customer);

            if (exitPoint != null)
                brain.MoveTo(exitPoint.position);
            else
                CompleteState();
        }

        public override void Tick()
        {
            if (exitPoint == null)
                return;

            if (brain.HasReachedDestination())
                CompleteState();
        }
    }
}
