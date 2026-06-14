using UnityEngine;

namespace Cherry.ArcadeAI
{
    /// <summary>
    /// Final customer state.
    /// Posts a review, updates capacity, then despawns the customer.
    /// </summary>
    public class CustomerPostReviewState : ArcadeAIState
    {
        [Header("Despawn")]
        public bool destroyCustomerRoot = true;
        public float destroyDelay;

        public override void Enter()
        {
            base.Enter();

            CustomerBrain customer = brain as CustomerBrain;

            if (customer != null)
            {
                PostReview(customer);
                customer.RegisterLeftIfNeeded();
            }

            if (destroyCustomerRoot)
                Destroy(brain.gameObject, Mathf.Max(0f, destroyDelay));
        }

        private void PostReview(CustomerBrain customer)
        {
            if (customer.reviewMemory == null || ArcadeReputationManager.Instance == null)
                return;

            int day = ArcadeReputationManager.Instance.currentDay;
            CustomerReview review = customer.reviewMemory.GenerateReview(customer.displayName, day);

            ArcadeReputationManager.Instance.PostReview(review);
        }
    }
}
