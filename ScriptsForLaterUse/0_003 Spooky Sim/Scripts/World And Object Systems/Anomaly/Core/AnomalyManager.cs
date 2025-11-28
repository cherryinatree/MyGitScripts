using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cherry.Anomalies
{
    /// <summary>
    /// Master anomaly controller. Anomalies subscribe/unsubscribe automatically.
    /// Responsible for selecting and activating anomalies.
    /// </summary>
    [System.Serializable]
    public class AnomalyManager 
    {
        public static AnomalyManager Instance 
        { 
            get 
            {
                if (_instance == null)
                {
                    _instance = new AnomalyManager();
                }
                return _instance;
            } 
            private set { _instance = value; }
        }

        private static AnomalyManager _instance;

        [Header("Runtime")]
        [SerializeField] private int maxActiveAnomalies = 1;
        [SerializeField] private bool autoActivateOnStart = true;
        [SerializeField] private float autoActivateDelay = 1f;

        private List<AnomalyBase> all = new();
        private readonly List<AnomalyBase> active = new();
        private readonly HashSet<string> completedNonRepeatables = new();

        public List<AnomalyBase> All 
        {
            get { return all; }
        }
        public IReadOnlyList<AnomalyBase> Active => active;

        public event Action<AnomalyBase> OnAnyActivated;
        public event Action<AnomalyBase> OnAnyResolved;

     /*   private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (autoActivateOnStart)
                Invoke(nameof(ActivateRandom), autoActivateDelay);
        }*/

        public void Register(AnomalyBase anomaly)
        {
            if (anomaly == null || all.Contains(anomaly)) return;
            all.Add(anomaly);
            anomaly.OnActivated += HandleActivated;
            anomaly.OnResolved += HandleResolved;
        }

        public void Unregister(AnomalyBase anomaly)
        {
            if (anomaly == null) return;
            all.Remove(anomaly);
            active.Remove(anomaly);
            anomaly.OnActivated -= HandleActivated;
            anomaly.OnResolved -= HandleResolved;
        }

        public void MarkCompleted(AnomalyBase anomaly)
        {
            if (anomaly != null) completedNonRepeatables.Add(anomaly.AnomalyId);
        }

        public bool IsCompletedNonRepeatable(string id) => completedNonRepeatables.Contains(id);

        public void ActivateRandom(string roomId = null)
        {
            if (active.Count >= maxActiveAnomalies) return;

            var candidates = all
                .Where(a => a.State == AnomalyState.Armed)
                .Where(a => a.CanRepeat || !IsCompletedNonRepeatable(a.AnomalyId))
                .Where(a => roomId == null || a.RoomId == roomId)
                .ToList();

            if (candidates.Count == 0) return;

            var chosen = WeightedPick(candidates);
            chosen.Activate();
        }

        public void ForceActivate(string anomalyId)
        {
            var a = all.FirstOrDefault(x => x.AnomalyId == anomalyId);
            if (a != null && a.State == AnomalyState.Armed) a.Activate();
        }

        public void DeactivateAll()
        {
            foreach (var a in active.ToArray())
                a.Deactivate();
        }

        private void HandleActivated(AnomalyBase anomaly)
        {
            if (!active.Contains(anomaly)) active.Add(anomaly);
            OnAnyActivated?.Invoke(anomaly);
        }

        private void HandleResolved(AnomalyBase anomaly)
        {
            active.Remove(anomaly);
            OnAnyResolved?.Invoke(anomaly);

            // Auto-fill another anomaly if there’s space
            if (autoActivateOnStart && active.Count < maxActiveAnomalies)
                ActivateRandom(anomaly.RoomId);
        }

        private AnomalyBase WeightedPick(List<AnomalyBase> list)
        {
            float total = 0f;
            foreach (var a in list) total += Mathf.Max(0.0001f, a.Weight);

            float roll = UnityEngine.Random.value * total;
            float acc = 0f;

            foreach (var a in list)
            {
                acc += Mathf.Max(0.0001f, a.Weight);
                if (roll <= acc) return a;
            }

            return list[list.Count - 1];
        }
    }
}
