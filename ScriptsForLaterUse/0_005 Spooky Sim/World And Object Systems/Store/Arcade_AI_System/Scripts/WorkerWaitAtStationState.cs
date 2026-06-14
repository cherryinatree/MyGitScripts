using UnityEngine;

namespace Cherry.ArcadeAI
{
    /// <summary>
    /// Generic worker idle/service state.
    /// The station itself handles customer service timing.
    /// This state keeps the worker at the assigned station and exposes animation hooks.
    /// </summary>
    public class WorkerWaitAtStationState : ArcadeAIState
    {
        [Header("Animator Parameters")]
        public string busyBoolParameter = "Busy";
        public string stationTypeIntParameter = "StationType";

        public override void Enter()
        {
            base.Enter();
            MoveToStation();
            UpdateAnimator();
        }

        public override void Tick()
        {
            MoveToStation();
            UpdateAnimator();
        }

        public override void Exit()
        {
            SetBusyAnimator(false);
            base.Exit();
        }

        private void MoveToStation()
        {
            WorkerBrain worker = brain as WorkerBrain;

            if (worker == null || worker.assignedStation == null)
                return;

            brain.MoveTo(worker.assignedStation.GetWorkerStandWorldPosition());
        }

        private void UpdateAnimator()
        {
            WorkerBrain worker = brain as WorkerBrain;

            if (worker == null || brain.animator == null)
                return;

            SetBusyAnimator(worker.busy);

            if (!string.IsNullOrWhiteSpace(stationTypeIntParameter) && worker.assignedStation != null)
                brain.animator.SetInteger(stationTypeIntParameter, (int)worker.assignedStation.stationType);
        }

        private void SetBusyAnimator(bool busy)
        {
            if (brain == null || brain.animator == null || string.IsNullOrWhiteSpace(busyBoolParameter))
                return;

            brain.animator.SetBool(busyBoolParameter, busy);
        }
    }
}
