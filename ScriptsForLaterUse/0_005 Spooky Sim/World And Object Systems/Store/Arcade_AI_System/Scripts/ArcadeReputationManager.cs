using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Cherry.ArcadeAI
{
    /// <summary>
    /// Stores reviews, calculates the rolling arcade star rating,
    /// and provides visitor interest multipliers for daily spawning.
    /// </summary>
    public class ArcadeReputationManager : MonoBehaviour
    {
        public static ArcadeReputationManager Instance { get; private set; }

        [Header("Rating")]
        [Range(1f, 5f)] public float currentStarRating = 3f;
        public int maxStoredReviews = 30;

        [Header("Current Day")]
        public int currentDay = 1;

        [Header("Visitor Multipliers")]
        public float oneStarMultiplier = 0.20f;
        public float twoStarMultiplier = 0.40f;
        public float threeStarMultiplier = 0.65f;
        public float fourStarMultiplier = 0.85f;
        public float fiveStarMultiplier = 1.10f;

        [Header("Reviews")]
        public List<CustomerReview> recentReviews = new List<CustomerReview>();

        [Header("Events")]
        public UnityEvent onReviewPosted;
        public UnityEvent onRatingChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void SetCurrentDay(int day)
        {
            currentDay = Mathf.Max(1, day);
        }

        public void PostReview(CustomerReview review)
        {
            if (review == null)
                return;

            recentReviews.Add(review);

            while (recentReviews.Count > Mathf.Max(1, maxStoredReviews))
                recentReviews.RemoveAt(0);

            float oldRating = currentStarRating;
            RecalculateStarRating();

            onReviewPosted?.Invoke();

            if (!Mathf.Approximately(oldRating, currentStarRating))
                onRatingChanged?.Invoke();

            Debug.Log($"New arcade review: {review.stars} stars - {review.reviewText}", this);
        }

        public void ClearReviews()
        {
            recentReviews.Clear();
            currentStarRating = 3f;
            onRatingChanged?.Invoke();
        }

        public int GetTargetVisitorsToday(int maxCapacity)
        {
            float multiplier = GetRatingMultiplier();
            return Mathf.Max(1, Mathf.RoundToInt(maxCapacity * multiplier));
        }

        public float GetRatingMultiplier()
        {
            if (currentStarRating < 1.5f) return oneStarMultiplier;
            if (currentStarRating < 2.5f) return twoStarMultiplier;
            if (currentStarRating < 3.5f) return threeStarMultiplier;
            if (currentStarRating < 4.5f) return fourStarMultiplier;

            return fiveStarMultiplier;
        }

        private void RecalculateStarRating()
        {
            if (recentReviews.Count == 0)
                return;

            float total = 0f;

            for (int i = 0; i < recentReviews.Count; i++)
                total += recentReviews[i].stars;

            currentStarRating = Mathf.Clamp(total / recentReviews.Count, 1f, 5f);
        }
    }
}
