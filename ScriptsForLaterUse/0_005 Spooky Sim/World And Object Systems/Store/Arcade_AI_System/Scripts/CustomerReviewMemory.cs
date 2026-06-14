using System.Collections.Generic;
using UnityEngine;

namespace Cherry.ArcadeAI
{
    /// <summary>
    /// Tracks what happened to one customer during their visit.
    /// Stations, mess systems, broken games, long queues, etc. should report to this.
    /// </summary>
    public class CustomerReviewMemory : MonoBehaviour
    {
        [Header("Scores")]
        [Range(0, 100)] public int funScore = 100;
        [Range(0, 100)] public int foodScore = 100;
        [Range(0, 100)] public int cleanlinessScore = 100;
        [Range(0, 100)] public int waitScore = 100;
        [Range(0, 100)] public int valueScore = 100;

        [Header("Visit Notes")]
        public List<string> positiveNotes = new List<string>();
        public List<string> negativeNotes = new List<string>();
        public List<string> badEvents = new List<string>();

        [Header("Generated Review")]
        [TextArea(2, 5)] public string lastGeneratedReview;

        public void AddPositive(string note)
        {
            if (string.IsNullOrWhiteSpace(note))
                return;

            if (!positiveNotes.Contains(note))
                positiveNotes.Add(note);
        }

        public void AddComplaint(string complaint, int penalty = 10, ArcadeReviewIssueType issueType = ArcadeReviewIssueType.Positive)
        {
            if (string.IsNullOrWhiteSpace(complaint))
                return;

            if (!negativeNotes.Contains(complaint))
                negativeNotes.Add(complaint);

            ApplyPenalty(issueType, penalty);
        }

        public void AddBadEvent(string badEvent, int penalty = 20, ArcadeReviewIssueType issueType = ArcadeReviewIssueType.Positive)
        {
            if (string.IsNullOrWhiteSpace(badEvent))
                return;

            if (!badEvents.Contains(badEvent))
                badEvents.Add(badEvent);

            ApplyPenalty(issueType, penalty);
        }

        public CustomerReview GenerateReview(string customerName, int day)
        {
            int stars = CalculateStars();
            string reason = GetMainReason();
            string text = BuildReviewText(stars, reason);

            lastGeneratedReview = text;

            CustomerReview review = new CustomerReview
            {
                stars = stars,
                reviewText = text,
                mainReason = reason,
                customerName = string.IsNullOrWhiteSpace(customerName) ? "Customer" : customerName,
                dayPosted = day
            };

            return review;
        }

        public int CalculateStars()
        {
            int stars = 5;

            if (funScore < 80) stars--;
            if (foodScore < 75) stars--;
            if (cleanlinessScore < 75) stars--;
            if (waitScore < 75) stars--;
            if (valueScore < 75) stars--;
            if (badEvents.Count > 0) stars--;

            return Mathf.Clamp(stars, 1, 5);
        }

        private void ApplyPenalty(ArcadeReviewIssueType issueType, int penalty)
        {
            penalty = Mathf.Abs(penalty);

            switch (issueType)
            {
                case ArcadeReviewIssueType.StoreDirty:
                    cleanlinessScore = Mathf.Clamp(cleanlinessScore - penalty, 0, 100);
                    break;

                case ArcadeReviewIssueType.WaitedTooLong:
                case ArcadeReviewIssueType.FoodTookTooLong:
                case ArcadeReviewIssueType.NoWorkerAvailable:
                    waitScore = Mathf.Clamp(waitScore - penalty, 0, 100);
                    break;

                case ArcadeReviewIssueType.TooExpensive:
                    valueScore = Mathf.Clamp(valueScore - penalty, 0, 100);
                    break;

                case ArcadeReviewIssueType.FoodUnavailable:
                    foodScore = Mathf.Clamp(foodScore - penalty, 0, 100);
                    break;

                default:
                    funScore = Mathf.Clamp(funScore - penalty, 0, 100);
                    break;
            }
        }

        private string GetMainReason()
        {
            if (badEvents.Count > 0) return badEvents[0];
            if (negativeNotes.Count > 0) return negativeNotes[0];
            if (positiveNotes.Count > 0) return positiveNotes[0];

            return "Had a normal visit";
        }

        private string BuildReviewText(int stars, string reason)
        {
            if (stars == 5)
            {
                if (positiveNotes.Count > 0)
                    return $"Had a great time! {positiveNotes[0]}.";

                return "Had a great time! The arcade was fun and there was plenty to do.";
            }

            if (stars == 4)
                return $"Pretty good arcade, but {LowerFirst(reason)}.";

            if (stars == 3)
                return $"It was okay. {reason} made the visit less fun.";

            if (stars == 2)
                return $"Not a great visit. {reason} really bothered me.";

            return $"Bad experience. {reason} ruined the visit.";
        }

        private string LowerFirst(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            if (value.Length == 1)
                return value.ToLower();

            return char.ToLower(value[0]) + value.Substring(1);
        }
    }
}
