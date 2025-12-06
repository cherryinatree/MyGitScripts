using System;
using System.Collections.Generic;
using UnityEngine;

namespace Remodeling.Runtime
{
    public class PlayerUpgradeState : MonoBehaviour
    {
        [Serializable]
        public struct UpgradePurchase
        {
            public string id;
            public int count;
        }

        [Header("Economy")]
        [SerializeField] private int credits = 2000;

        [Header("Progress")]
        [SerializeField] private int sizeTier = 0;

        [SerializeField] private List<UpgradePurchase> purchases = new();

        private Dictionary<string, int> _counts;

        public event Action OnChanged;

        public int Credits => credits;
        public int SizeTier => sizeTier;
        public IReadOnlyList<UpgradePurchase> Purchases => purchases;

        private void Awake() => RebuildCounts();

        private void Start()
        {

            credits = SaveData.Current.mainData.playerData.money;
        }

        private void RebuildCounts()
        {
            _counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var p in purchases)
            {
                if (string.IsNullOrWhiteSpace(p.id)) continue;
                _counts[p.id] = Mathf.Max(0, p.count);
            }
        }

        public int GetCount(string id)
            => (!string.IsNullOrWhiteSpace(id) && _counts.TryGetValue(id, out var c)) ? c : 0;

        public bool Owns(string id) => GetCount(id) > 0;

        public bool TrySpend(int amount)
        {
            if (amount < 0 || credits < amount) return false;
            SaveData.Current.mainData.playerData.money -= amount;
            credits = SaveData.Current.mainData.playerData.money;
            OnChanged?.Invoke();
            return true;
        }
        public void AddCredits(int amount)
        {
            SaveData.Current.mainData.playerData.money += amount;
            credits = SaveData.Current.mainData.playerData.money;
            OnChanged?.Invoke();
        }


        public void SetSizeTier(int tier)
        {
            tier = Mathf.Max(0, tier);
            if (tier == sizeTier) return;
            sizeTier = tier;
            OnChanged?.Invoke();
        }

        public void AddPurchase(string id, int delta = 1)
        {
            if (string.IsNullOrWhiteSpace(id) || delta <= 0) return;

            int current = GetCount(id);
            int next = current + delta;

            _counts[id] = next;

            bool updated = false;
            for (int i = 0; i < purchases.Count; i++)
            {
                if (purchases[i].id == id)
                {
                    purchases[i] = new UpgradePurchase { id = id, count = next };
                    updated = true;
                    break;
                }
            }
            if (!updated)
                purchases.Add(new UpgradePurchase { id = id, count = next });

            OnChanged?.Invoke();
        }

        // Hook your save system here (serialize 'credits', 'sizeTier', and 'purchases').
    }
}

