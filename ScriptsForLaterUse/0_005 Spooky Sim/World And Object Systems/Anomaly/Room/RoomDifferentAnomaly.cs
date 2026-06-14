using UnityEngine;
using System;
using System.Linq;

namespace Cherry.Anomalies
{
    public class RoomDifferentAnomaly : AnomalyBase
    {
        [SerializeField] private int objectsToAffect = 3;
        [SerializeField] private RoomObjectAnomalyTarget[] targets;

        private System.Random rng;
        private RoomObjectAnomalyTarget[] affected;

        protected override void Activate_Internal()
        {
            if (targets == null || targets.Length == 0)
                targets = GetComponentsInChildren<RoomObjectAnomalyTarget>(true);

            rng = new System.Random(UnityEngine.Random.Range(int.MinValue, int.MaxValue));

            affected = targets
                .OrderBy(_ => rng.Next())
                .Take(Mathf.Min(objectsToAffect, targets.Length))
                .ToArray();

            foreach (var t in affected)
                t.ApplyRandomVariant(rng);
        }

        protected override void Deactivate_Internal()
        {
            if (affected == null) return;
            foreach (var t in affected)
                t.RestoreNormal();

            affected = null;
        }

        protected override bool CheckResolved_Internal() => false;
    }
}
