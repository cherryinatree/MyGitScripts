using System;
using UnityEngine;

namespace Cherry.ArcadeAI
{
    [Serializable]
    public class CustomerReview
    {
        [Range(1, 5)] public int stars = 5;
        public string reviewText;
        public string mainReason;
        public string customerName;
        public int dayPosted;
    }
}
