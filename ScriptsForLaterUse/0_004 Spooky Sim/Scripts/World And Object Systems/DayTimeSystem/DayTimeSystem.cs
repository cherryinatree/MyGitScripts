using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cherry.DayAndTime
{
    public enum DayPhase
    {
        PreOpen,   // Frozen at 10:00 AM until player opens the store
        Open,      // Time ticks
        Closed     // Frozen at 9:00 PM (player can do night stuff + sleep)
    }

    [Serializable]
    public class DayReport
    {
        public int dayNumber;
        public int minutesOpen;
        public int customers;
        public int transactions;
        public float revenue;
        public float profit;
        public string notes;
    }

    public interface IStoreReportSource
    {
        DayReport BuildDayReport(int dayNumber, int minutesOpen);
    }

    [DisallowMultipleComponent]
    public class DayTimeSystem : MonoBehaviour
    {
        public static DayTimeSystem Instance { get; private set; }

        [Header("Clock Settings")]
        [SerializeField] private int openHour = 10;
        [SerializeField] private int openMinute = 0;
        [SerializeField] private int closeHour = 21;  // 9 PM
        [SerializeField] private int closeMinute = 0;

        [Tooltip("Only used while store is open. Example: 0.5 means 1 in-game minute passes every 0.5 seconds.")]
        [SerializeField, Min(0.01f)] private float secondsPerGameMinute = 0.5f;

        [Header("Optional: Report Source (implements IStoreReportSource)")]
        [SerializeField] private MonoBehaviour reportSource;

        [Header("Persistence (optional)")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        // State
        [SerializeField] private int dayNumber = 1;
        [SerializeField] private DayPhase phase = DayPhase.PreOpen;
        [SerializeField] private int minuteOfDay; // 0..1439
        [SerializeField] private bool openedToday;

        private float _accum;
        private int _minutesOpenToday;
        private readonly HashSet<string> _usedDailyActions = new();

        // Events
        public event Action<int> OnNewDayStarted;                // dayNumber
        public event Action<DayPhase> OnPhaseChanged;            // phase
        public event Action<int> OnTimeChanged;                  // minuteOfDay
        public event Action<int> OnStoreOpened;                  // dayNumber
        public event Action<DayReport> OnStoreClosed;            // report

        private IStoreReportSource _reportSource;

        public int DayNumber => dayNumber;
        public DayPhase Phase => phase;
        public int MinuteOfDay => minuteOfDay;

        public int OpenTimeMinutes => openHour * 60 + openMinute;
        public int CloseTimeMinutes => closeHour * 60 + closeMinute;

        public bool CanOpenStore => phase == DayPhase.PreOpen && !openedToday;
        public bool CanCloseStore => phase == DayPhase.Open;
        public bool CanSleep => phase == DayPhase.Closed && minuteOfDay >= CloseTimeMinutes;

        public string TimeString => FormatTime(minuteOfDay);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

            _reportSource = reportSource as IStoreReportSource;
            if (reportSource != null && _reportSource == null)
            {
                Debug.LogWarning($"{nameof(DayTimeSystem)}: Report Source is assigned but does not implement IStoreReportSource.", this);
            }

            // Initialize frozen time based on phase
            if (minuteOfDay <= 0)
                minuteOfDay = (phase == DayPhase.Closed) ? CloseTimeMinutes : OpenTimeMinutes;

            // Force frozen “10 AM until open” rule
            if (phase == DayPhase.PreOpen)
                minuteOfDay = OpenTimeMinutes;

            if (phase == DayPhase.Closed)
                minuteOfDay = CloseTimeMinutes;

            OnTimeChanged?.Invoke(minuteOfDay);
            OnPhaseChanged?.Invoke(phase);
        }

        private void Update()
        {
            if (phase != DayPhase.Open) return;

            _accum += Time.deltaTime;
            while (_accum >= secondsPerGameMinute)
            {
                _accum -= secondsPerGameMinute;
                AdvanceMinutes(1);
            }
        }

        private void AdvanceMinutes(int delta)
        {
            minuteOfDay = Mathf.Clamp(minuteOfDay + delta, 0, 24 * 60 - 1);
            _minutesOpenToday += delta;

            OnTimeChanged?.Invoke(minuteOfDay);

            // Auto-close exactly at/after close time
            if (minuteOfDay >= CloseTimeMinutes)
            {
                CloseStore(jumpToCloseTime: true, autoClosed: true);
            }
        }

        public void OpenStore()
        {
            if (!CanOpenStore) return;

            openedToday = true;
            _minutesOpenToday = 0;
            _accum = 0f;

            SetPhase(DayPhase.Open);

            // Start the “day clock” at 10:00 AM
            minuteOfDay = OpenTimeMinutes;
            OnTimeChanged?.Invoke(minuteOfDay);

            OnStoreOpened?.Invoke(dayNumber);
        }

        public void CloseStore(bool jumpToCloseTime = true, bool autoClosed = false)
        {
            if (phase != DayPhase.Open && !autoClosed) return;

            if (jumpToCloseTime)
            {
                minuteOfDay = CloseTimeMinutes;
                OnTimeChanged?.Invoke(minuteOfDay);
            }

            SetPhase(DayPhase.Closed);

            // Build report (optional)
            DayReport report = _reportSource != null
                ? _reportSource.BuildDayReport(dayNumber, _minutesOpenToday)
                : new DayReport
                {
                    dayNumber = dayNumber,
                    minutesOpen = _minutesOpenToday,
                    notes = "(No report source assigned)"
                };

            OnStoreClosed?.Invoke(report);
        }

        public void SleepToNextDay()
        {
            if (!CanSleep) return;

            dayNumber += 1;
            openedToday = false;
            _minutesOpenToday = 0;
            _accum = 0f;

            _usedDailyActions.Clear();

            SetPhase(DayPhase.PreOpen);

            // New day starts frozen at 10:00 AM again
            minuteOfDay = OpenTimeMinutes;
            OnTimeChanged?.Invoke(minuteOfDay);

            OnNewDayStarted?.Invoke(dayNumber);
        }

        private void SetPhase(DayPhase newPhase)
        {
            if (phase == newPhase) return;
            phase = newPhase;

            // Enforce freeze times
            if (phase == DayPhase.PreOpen) minuteOfDay = OpenTimeMinutes;
            if (phase == DayPhase.Closed) minuteOfDay = CloseTimeMinutes;

            OnPhaseChanged?.Invoke(phase);
        }

        // -------- Once-per-day actions --------

        public bool HasUsedDailyAction(string actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId)) return false;
            return _usedDailyActions.Contains(actionId.Trim());
        }

        public bool TryUseDailyAction(string actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId)) return false;
            actionId = actionId.Trim();

            if (_usedDailyActions.Contains(actionId)) return false;
            _usedDailyActions.Add(actionId);
            return true;
        }

        // -------- Helpers --------

        public static string FormatTime(int minutesFromMidnight)
        {
            minutesFromMidnight = Mathf.Clamp(minutesFromMidnight, 0, 24 * 60 - 1);
            int h24 = minutesFromMidnight / 60;
            int m = minutesFromMidnight % 60;

            bool pm = h24 >= 12;
            int h12 = h24 % 12;
            if (h12 == 0) h12 = 12;

            return $"{h12}:{m:00} {(pm ? "PM" : "AM")}";
        }
    }
}
