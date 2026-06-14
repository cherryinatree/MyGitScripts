using UnityEngine;
using UnityEngine.Events;

namespace Cherry.ArcadeAI
{
    /// <summary>
    /// Simple helper you can call from an interact button, UI button, or trigger.
    /// Assigns a worker to a station.
    /// </summary>
    public class WorkerAssignmentZone : MonoBehaviour
    {
        public ArcadeStation stationToAssign;
        public UnityEvent onWorkerAssigned;

        public void AssignWorker(WorkerBrain worker)
        {
            if (worker == null || stationToAssign == null)
                return;

            worker.AssignToStation(stationToAssign);
            onWorkerAssigned?.Invoke();
        }

        private void OnTriggerEnter(Collider other)
        {
            WorkerBrain worker = other.GetComponentInParent<WorkerBrain>();

            if (worker != null)
                AssignWorker(worker);
        }
    }
}
