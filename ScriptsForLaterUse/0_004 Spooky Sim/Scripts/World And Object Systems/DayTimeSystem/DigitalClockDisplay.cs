using TMPro;
using UnityEngine;

#if TMP_PRESENT || TEXTMESHPRO_PRESENT
using TMPro;
#endif

namespace Cherry.DayAndTime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cherry/Day And Time/Digital Clock Display")]
    public class DigitalClockDisplay : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private DayTimeSystem dayTime;

        [Header("Output (assign one)")]
#if TMP_PRESENT || TEXTMESHPRO_PRESENT
        [SerializeField] private TMP_Text tmpText;
#endif
        [SerializeField] private TextMeshProUGUI uiText;
        [SerializeField] private TextMeshPro ClockText;


        [Header("Display Options")]
        [SerializeField] private bool showDayNumber = false;
        [SerializeField] private bool showPhase = false;
        [SerializeField] private string prefix = "";
        [SerializeField] private string suffix = "";


        private void Awake()
        {
            if (dayTime == null) dayTime = DayTimeSystem.Instance;

#if TMP_PRESENT || TEXTMESHPRO_PRESENT
            if (tmpText == null) tmpText = GetComponent<TMP_Text>();
#endif
            if (uiText == null) uiText = GetComponent<TextMeshProUGUI>();

            Refresh();
        }

        private void OnEnable()
        {
            if (dayTime == null) dayTime = DayTimeSystem.Instance;
            if (dayTime == null) return;

            dayTime.OnTimeChanged += HandleTimeChanged;
            dayTime.OnPhaseChanged += HandlePhaseChanged;
            dayTime.OnNewDayStarted += HandleNewDay;

            Refresh();
        }

        private void OnDisable()
        {
            if (dayTime == null) return;

            dayTime.OnTimeChanged -= HandleTimeChanged;
            dayTime.OnPhaseChanged -= HandlePhaseChanged;
            dayTime.OnNewDayStarted -= HandleNewDay;
        }

        private void HandleTimeChanged(int _) => Refresh();
        private void HandlePhaseChanged(DayPhase _) => Refresh();
        private void HandleNewDay(int _) => Refresh();

        public void Refresh()
        {
            if (dayTime == null) return;

            string t = dayTime.TimeString;

            string dayPart = showDayNumber ? $"Day {dayTime.DayNumber} " : "";
            string phasePart = showPhase ? $"[{dayTime.Phase}] " : "";

            string final = $"{prefix}{dayPart}\n{phasePart}{t}{suffix}";
            SetText(final);
        }

        private void SetText(string value)
        {
#if TMP_PRESENT || TEXTMESHPRO_PRESENT
            if (tmpText != null) { tmpText.text = value; return; }
#endif
             if (uiText != null) { uiText.text = value; return; }
            
             if (ClockText != null) { ClockText.text = value; return; }
            
            // Nothing assigned; fail silently.
        }
    }
}
