using UnityEngine;

namespace Cherry.ArcadeAI
{
    public class WorkerGoToAssignedStationState : ArcadeAIState
    {
        [Header("Idle")]
        public bool keepTryingUntilAssigned = true;

        public override void Enter()
        {
            base.Enter();

            WorkerBrain worker = brain as WorkerBrain;
            if (worker == null || worker.assignedStation == null)
            {
                if (!keepTryingUntilAssigned)
                    CompleteState();

                return;
            }

            brain.MoveTo(worker.assignedStation.GetWorkerStandWorldPosition());
        }

        public override void Tick()
        {
            WorkerBrain worker = brain as WorkerBrain;

            if (worker == null)
                return;

            if (worker.assignedStation == null)
            {
                if (!keepTryingUntilAssigned)
                    CompleteState();

                return;
            }

            brain.MoveTo(worker.assignedStation.GetWorkerStandWorldPosition());

            if (brain.HasReachedDestination())
                CompleteState();
        }
    }
}
