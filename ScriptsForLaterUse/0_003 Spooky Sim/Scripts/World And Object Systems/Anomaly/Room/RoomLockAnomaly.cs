using UnityEngine;

namespace Cherry.Anomalies
{
    public class RoomLockAnomaly : AnomalyBase
    {
        [SerializeField] private MonoBehaviour[] lockables; // your Door/Lock scripts
        [SerializeField] private BoolCondition resolveCondition; // simple SO or component

        protected override void Activate_Internal()
        {
            foreach (var l in lockables)
                l.enabled = true; // or l.Lock()
        }

        protected override void Deactivate_Internal()
        {
            foreach (var l in lockables)
                l.enabled = false; // or l.Unlock()
        }

        protected override bool CheckResolved_Internal()
        {
            return resolveCondition != null && resolveCondition.IsTrue;
        }
    }

    // Tiny example condition wrapper (swap with your SaveData flag conditions later)
    public class BoolCondition : MonoBehaviour
    {
        public bool IsTrue;
    }
}
