using UnityEngine;

namespace Cherry.ArcadeAI
{
    /// <summary>
    /// Worker-specific data.
    /// Assign workers to any ArcadeStation from the inspector or at runtime.
    /// </summary>
    public class WorkerBrain : ArcadeAgentBrain
    {
        [Header("Worker")]
        public WorkerRole role = WorkerRole.General;
        public ArcadeStation assignedStation;

        [Header("Runtime")]
        public bool busy;
        public CustomerBrain customerBeingServed;
        public ArcadeStation stationBeingWorked;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            if (assignedStation != null)
                AssignToStation(assignedStation);
        }

        public void AssignToStation(ArcadeStation station)
        {
            if (assignedStation == station)
                return;

            if (assignedStation != null)
                assignedStation.RemoveWorker(this);

            assignedStation = station;

            if (assignedStation != null)
                assignedStation.AssignWorker(this);
        }

        public void ClearAssignment()
        {
            if (assignedStation != null)
                assignedStation.RemoveWorker(this);

            assignedStation = null;
        }

        public void SetBusyWithCustomer(CustomerBrain customer, ArcadeStation station)
        {
            busy = true;
            customerBeingServed = customer;
            stationBeingWorked = station;
        }

        public void ClearBusyWithCustomer(CustomerBrain customer, ArcadeStation station)
        {
            if (stationBeingWorked != station)
                return;

            busy = false;
            customerBeingServed = null;
            stationBeingWorked = null;
        }
    }
}
