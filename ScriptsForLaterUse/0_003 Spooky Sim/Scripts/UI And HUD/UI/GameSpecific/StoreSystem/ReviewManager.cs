using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Review
{
    public string author;
    [Range(0, 5)] public float stars;
    [TextArea] public string text;
    public DateTime when;
}

public class ReviewManager : MonoBehaviour
{
    [SerializeField] private List<Review> reviews = new();

    public float AverageStars => reviews.Count == 0 ? 0f : reviews.Average(r => r.stars);
    public int Count => reviews.Count;
    public IReadOnlyList<Review> All => reviews;

    public void AddReview(Review r) => reviews.Add(r);

    // Optional: seed some reviews for demo
    [ContextMenu("Seed Sample Reviews")]
    private void Seed()
    {
        reviews = new List<Review> {
            new Review{ author="Jordan", stars=5, text="Fast delivery and great selection.", when=DateTime.Now.AddDays(-2)},
            new Review{ author="Avery", stars=4, text="Solid quality. Would order again.", when=DateTime.Now.AddDays(-1)},
            new Review{ author="Sam",    stars=3, text="Okay overall. Packaging could improve.", when=DateTime.Now},
        };
    }
}
