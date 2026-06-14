using UnityEngine;

namespace Cherry.ArcadeAI
{
    public class CustomerEnterArcadeState : ArcadeAIState
    {
        [Header("Entrance")]
        public Transform entryTarget;

        public override void Enter()
        {
            base.Enter();

            CustomerBrain customer = brain as CustomerBrain;
            if (customer != null)
                customer.RegisterEnteredIfNeeded();

            if (entryTarget != null)
                brain.MoveTo(entryTarget.position);
            else
                CompleteState();
        }

        public override void Tick()
        {
            if (entryTarget == null)
                return;

            if (brain.HasReachedDestination())
                CompleteState();
        }
    }
}
