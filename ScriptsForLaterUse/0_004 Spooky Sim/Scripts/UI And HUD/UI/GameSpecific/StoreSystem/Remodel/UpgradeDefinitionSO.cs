using System;
using UnityEngine;
using Remodeling.Runtime;
using Remodeling.Data;


namespace Remodeling.Data
{
    public enum UpgradeCategory
    {
        Size,
        Feature,
        Automation,
        Decor,
        Utility,
        Collector,
        ArHeadset,
        Rig,
        Misc
    }
}


namespace Remodeling.Data
{
    [CreateAssetMenu(menuName = "Remodel/Upgrade Definition")]
    public class UpgradeDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public UpgradeCategory category;

        [Header("Economy")]
        public int baseCost = 500;

        [Tooltip("Optional. 1 = same price each time. 1.15 = 15% more each purchase.")]
        public float costGrowth = 1f;

        [Header("Rules")]
        [Tooltip("Must own ALL of these before purchase.")]
        public string[] requiredUpgradeIds;

        [Tooltip("1=buy once, 0=infinite, N=up to N times.")]
        public int maxPurchases = 1;

        [Header("Effects")]
        public RemodelAction[] actions;

        public int NextCost(int currentCount)
        {
            currentCount = Mathf.Max(0, currentCount);
            return Mathf.RoundToInt(baseCost * Mathf.Pow(Mathf.Max(0.0001f, costGrowth), currentCount));
        }

        public bool IsMaxed(int currentCount)
        {
            if (maxPurchases <= 0) return false; // infinite
            return currentCount >= maxPurchases;
        }
    }
}
