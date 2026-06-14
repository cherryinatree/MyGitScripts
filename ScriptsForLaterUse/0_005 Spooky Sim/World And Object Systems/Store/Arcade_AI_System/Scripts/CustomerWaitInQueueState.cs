using UnityEngine;

namespace Cherry.ArcadeAI
{
    public class CustomerWaitInQueueState : ArcadeAIState
    {
        [Header("Waiting")]
        public float complaintAfterSeconds = 20f;
        public float giveUpAfterSeconds = 45f;
        public bool leaveIfNoWorker = true;
        public float noWorkerComplaintAfterSeconds = 15f;

        [Header("Transitions")]
        public ArcadeAIState failedState;

        private float waitTimer;
        private bool complainedAboutWait;
        private bool complainedAboutWorker;

        public override void Enter()
        {
            base.Enter();

            waitTimer = 0f;
            complainedAboutWait = false;
            complainedAboutWorker = false;
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
            waitTimer += Time.deltaTime;

            brain.MoveTo(station.GetQueueWorldPosition(customer));

            if (!complainedAboutWait && waitTimer >= complaintAfterSeconds)
            {
                complainedAboutWait = true;
                customer.reviewMemory.AddComplaint("Waited too long in line", 15, ArcadeReviewIssueType.WaitedTooLong);
            }

            if (!station.allowUseWithoutWorker && station.AssignedWorker == null && waitTimer >= noWorkerComplaintAfterSeconds && !complainedAboutWorker)
            {
                complainedAboutWorker = true;
                customer.reviewMemory.AddComplaint("There was no worker available", 20, ArcadeReviewIssueType.NoWorkerAvailable);
            }

            if (waitTimer >= giveUpAfterSeconds)
            {
                station.LeaveQueue(customer);
                customer.FailCurrentActivity("Waited too long and gave up", 20, ArcadeReviewIssueType.WaitedTooLong);
                ChangeTo(failedState);
                return;
            }

            if (station.CanServeCustomer(customer))
                CompleteState();
        }
    }
}
