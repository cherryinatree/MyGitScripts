using UnityEngine;
using Cherry.DayAndTime;

namespace Cherry.Gameplay
{
    public class DailyActionGate : MonoBehaviour
    {
        [SerializeField] private string actionId = "unique_action_id";
        [SerializeField] private bool consumeOnTry = true;

        public bool CanDoToday()
        {
            var sys = DayTimeSystem.Instance;
            return sys != null && !sys.HasUsedDailyAction(actionId);
        }

        public bool TryDoToday()
        {
            var sys = DayTimeSystem.Instance;
            if (sys == null) return false;

            if (!consumeOnTry) return CanDoToday();
            return sys.TryUseDailyAction(actionId);
        }
    }
}
