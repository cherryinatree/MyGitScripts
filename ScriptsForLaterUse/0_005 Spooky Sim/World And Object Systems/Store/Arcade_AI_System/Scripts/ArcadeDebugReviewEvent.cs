using UnityEngine;

namespace Cherry.ArcadeAI
{
    /// <summary>
    /// Debug/helper component.
    /// Put this on dirty floors, broken games, long-wait triggers, etc.,
    /// then call ApplyComplaint(customerBrain) from Unity Events or another script.
    /// </summary>
    public class ArcadeDebugReviewEvent : MonoBehaviour
    {
        public string complaint = "Something bad happened";
        public int penalty = 10;
        public ArcadeReviewIssueType issueType = ArcadeReviewIssueType.Positive;
        public bool badEvent;

        public void ApplyComplaint(CustomerBrain customer)
        {
            if (customer == null || customer.reviewMemory == null)
                return;

            if (badEvent)
                customer.reviewMemory.AddBadEvent(complaint, penalty, issueType);
            else
                customer.reviewMemory.AddComplaint(complaint, penalty, issueType);
        }

        public void ApplyPositive(CustomerBrain customer)
        {
            if (customer == null || customer.reviewMemory == null)
                return;

            customer.reviewMemory.AddPositive(complaint);
        }
    }
}
