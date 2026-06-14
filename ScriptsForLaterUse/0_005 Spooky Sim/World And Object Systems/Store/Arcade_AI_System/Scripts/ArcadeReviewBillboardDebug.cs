using UnityEngine;

namespace Cherry.ArcadeAI
{
    /// <summary>
    /// Optional debug display using OnGUI.
    /// Good for testing before you make your real review UI.
    /// </summary>
    public class ArcadeReviewBillboardDebug : MonoBehaviour
    {
        public bool show = true;
        public Vector2 position = new Vector2(20, 20);
        public Vector2 size = new Vector2(520, 260);

        private void OnGUI()
        {
            if (!show || ArcadeReputationManager.Instance == null)
                return;

            ArcadeReputationManager rep = ArcadeReputationManager.Instance;

            GUILayout.BeginArea(new Rect(position.x, position.y, size.x, size.y), GUI.skin.box);
            GUILayout.Label($"Arcade Rating: {rep.currentStarRating:0.00} stars");
            GUILayout.Label($"Stored Reviews: {rep.recentReviews.Count}");

            int start = Mathf.Max(0, rep.recentReviews.Count - 5);
            for (int i = start; i < rep.recentReviews.Count; i++)
            {
                CustomerReview review = rep.recentReviews[i];
                GUILayout.Label($"{review.stars}★ {review.customerName}: {review.reviewText}");
            }

            GUILayout.EndArea();
        }
    }
}
