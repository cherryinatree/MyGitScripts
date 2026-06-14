using UnityEngine;
using UnityEngine.Events;
using Cherry.DayAndTime;

namespace Cherry.Gameplay
{
    public class BedSleepInteractor : MonoBehaviour
    {
        [SerializeField] private DayTimeSystem dayTime;
        [SerializeField] private UnityEvent onSleepDenied;
        [SerializeField] private UnityEvent onSleep; // put your fade/transition here

        private void Awake()
        {
            if (dayTime == null) dayTime = DayTimeSystem.Instance;
        }

        public void TrySleep()
        {
            if (dayTime == null) return;

            if (!dayTime.CanSleep)
            {
                onSleepDenied?.Invoke();
                return;
            }

            onSleep?.Invoke();
            dayTime.SleepToNextDay();
        }
    }
}
