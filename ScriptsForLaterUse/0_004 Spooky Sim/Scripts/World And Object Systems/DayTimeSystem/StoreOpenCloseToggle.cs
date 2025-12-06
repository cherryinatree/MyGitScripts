using TMPro;
using UnityEngine;
using UnityEngine.Events;

#if TMP_PRESENT || TEXTMESHPRO_PRESENT
using TMPro;
#endif

namespace Cherry.DayAndTime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cherry/Day And Time/Store Open-Close Toggle")]
    public class StoreOpenCloseToggle : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private DayTimeSystem dayTime;

        [Header("Optional: Label to show action")]
#if TMP_PRESENT || TEXTMESHPRO_PRESENT
        [SerializeField] private TMP_Text tmpLabel;
#endif
        [SerializeField] private TextMeshProUGUI uiLabel;

        [Header("Label Text")]
        [SerializeField] private string openLabel = "Open Store";
        [SerializeField] private string closeLabel = "Close Store";
        [SerializeField] private string closedLabel = "Store Closed (Sleep)";

        [Header("Events")]
        public UnityEvent onOpened;
        public UnityEvent onClosed;
        public UnityEvent onDenied;

        private void Awake()
        {
            if (dayTime == null) dayTime = DayTimeSystem.Instance;
            RefreshLabel();
        }

        private void OnEnable()
        {
            if (dayTime == null) dayTime = DayTimeSystem.Instance;
            if (dayTime == null) return;

            dayTime.OnPhaseChanged += HandlePhase;
            dayTime.OnNewDayStarted += HandleNewDay;
            RefreshLabel();
        }

        private void OnDisable()
        {
            if (dayTime == null) return;
            dayTime.OnPhaseChanged -= HandlePhase;
            dayTime.OnNewDayStarted -= HandleNewDay;
        }

        private void HandlePhase(DayPhase _) => RefreshLabel();
        private void HandleNewDay(int _) => RefreshLabel();

        /// <summary>
        /// Call this from your interaction system.
        /// </summary>
        public void Interact()
        {
            if (dayTime == null) dayTime = DayTimeSystem.Instance;
            if (dayTime == null) return;

            // PreOpen => Open
            if (dayTime.CanOpenStore)
            {
                dayTime.OpenStore();
                onOpened?.Invoke();
                RefreshLabel();
                return;
            }

            // Open => Closed
            if (dayTime.CanCloseStore)
            {
                dayTime.CloseStore(jumpToCloseTime: true);
                onClosed?.Invoke();
                RefreshLabel();
                return;
            }

            onDenied?.Invoke();
            RefreshLabel();
        }

        public bool CanInteractNow()
        {
            if (dayTime == null) dayTime = DayTimeSystem.Instance;
            if (dayTime == null) return false;

            return dayTime.CanOpenStore || dayTime.CanCloseStore;
        }

        public string CurrentPrompt()
        {
            if (dayTime == null) dayTime = DayTimeSystem.Instance;
            if (dayTime == null) return "";

            if (dayTime.CanOpenStore) return openLabel;
            if (dayTime.CanCloseStore) return closeLabel;
            return closedLabel;
        }

        public void RefreshLabel()
        {
            string value = CurrentPrompt();

#if TMP_PRESENT || TEXTMESHPRO_PRESENT
            if (tmpLabel != null) { tmpLabel.text = value; return; }
#endif
            if (uiLabel != null) { uiLabel.text = value; }
        }
    }
}
