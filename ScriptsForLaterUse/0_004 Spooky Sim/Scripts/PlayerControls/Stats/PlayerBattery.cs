using System;
using UnityEngine;

namespace Cherry.Combat
{
    /// <summary>
    /// Shared battery resource for player equipment.
    /// Supports:
    /// - Instant costs (TryConsume)
    /// - Continuous drains (BeginDrain / EndDrain)
    /// - Optional regen / recharge delay
    /// - Events for UI / SFX / VFX
    /// </summary>
    public class PlayerBattery : MonoBehaviour
    {
        [Header("Battery")]
        [SerializeField, Min(1f)] private float maxBattery = 100f;
        [SerializeField] private float currentBattery;

        [Header("Drain Tuning")]
        [Tooltip("Multiplier to all drain. 1 = normal, <1 = efficient gear, >1 = power hungry.")]
        [SerializeField] private float drainMultiplier = 1f;

        [Header("Regen / Recharge (Optional)")]
        [SerializeField] private bool useRegen = false;
        [SerializeField] private bool DelayRegenTillAfterUse = false;
        [SerializeField] private bool WillNotRegenOnceDepleted = false;
        [SerializeField, Min(0f)] private float regenPerSecond = 5f;
        [SerializeField, Min(0f)] private float regenDelayAfterUse = 2f;
        private float lastDrainTime = -999f;

        [Header("Thresholds")]
        [Tooltip("Event fires once when battery drops below this fraction (0-1).")]
        [Range(0f, 1f)]
        [SerializeField] private float lowBatteryThreshold = 0.15f;
        private bool lowBatteryFired = false;

        [Header("Debug")]
        [SerializeField] private bool logChanges = false;

        // Active continuous drains (sum of rates)
        private float activeDrainPerSecond = 0f;

        // -------- Events --------
        /// <summary> current, max, normalized </summary>
        public event Action<float, float, float> OnBatteryChanged;

        /// <summary> Fired when battery hits 0. </summary>
        public event Action OnBatteryDepleted;

        /// <summary> Fired when battery rises above 0 after depletion. </summary>
        public event Action OnBatteryRestored;

        /// <summary> Fired once when crossing low threshold. </summary>
        public event Action OnLowBattery;

        // -------- Properties --------
        public float MaxBattery => maxBattery;
        public float CurrentBattery => currentBattery;
        public float BatteryNormalized => maxBattery <= 0 ? 0 : currentBattery / maxBattery;
        public bool IsDepleted { get; private set; }

        private void Awake()
        {
            currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);
            if (currentBattery <= 0f) currentBattery = maxBattery;
            BroadcastChanged();
        }

        private void Update()
        {
            if (!IsDepleted && activeDrainPerSecond > 0f)
            {
                DrainContinuous(activeDrainPerSecond * Time.deltaTime);
            }

            if (useRegen)
            {
                if (WillNotRegenOnceDepleted && IsDepleted)
                {
                    return;
                }

                if(!DelayRegenTillAfterUse)
                {
                    Recharge(regenPerSecond * Time.deltaTime);
                }
                else if (Time.time >= lastDrainTime + regenDelayAfterUse && currentBattery < maxBattery)
                {
                    if (activeDrainPerSecond <= 0f)
                    {
                        Recharge(regenPerSecond * Time.deltaTime);
                    }
                }
            }
        }

        // ---------------- Public API ----------------

        /// <summary>
        /// Instant battery cost (e.g., gun shot). Returns true if enough battery.
        /// </summary>
        public bool TryConsume(float amount)
        {
            if (amount <= 0f) return true;
            if (IsDepleted) return false;

            float final = amount * drainMultiplier;
            if (final <= 0f) return true;

            lastDrainTime = Time.time;
            SetBatteryInternal(currentBattery - final);

            if (logChanges)
                Debug.Log($"{name} consumed {final} battery (raw {amount}). Remaining: {currentBattery}");

            return !IsDepleted;
        }

        /// <summary>
        /// Add a continuous drain source (e.g., ghost vision active).
        /// Pass the same rate to EndDrain later.
        /// </summary>
        public void BeginDrain(float drainPerSecond)
        {
            drainPerSecond = Mathf.Max(0f, drainPerSecond);
            if (drainPerSecond <= 0f) return;

            activeDrainPerSecond += drainPerSecond;
            lastDrainTime = Time.time;

            if (logChanges)
                Debug.Log($"{name} begin drain +{drainPerSecond}/s. Total drain: {activeDrainPerSecond}/s");
        }

        /// <summary>
        /// Remove a continuous drain source.
        /// </summary>
        public void EndDrain(float drainPerSecond)
        {
            drainPerSecond = Mathf.Max(0f, drainPerSecond);
            if (drainPerSecond <= 0f) return;

            activeDrainPerSecond = Mathf.Max(0f, activeDrainPerSecond - drainPerSecond);

            if (logChanges)
                Debug.Log($"{name} end drain -{drainPerSecond}/s. Total drain: {activeDrainPerSecond}/s");
        }

        /// <summary>
        /// Recharge instantly or over time. Returns actual restored amount.
        /// </summary>
        public float Recharge(float amount)
        {
            if (amount <= 0f) return 0f;

            float before = currentBattery;
            SetBatteryInternal(currentBattery + amount);
            float restored = currentBattery - before;

            if (logChanges && restored > 0f)
                Debug.Log($"{name} recharged {restored} battery.");

            return restored;
        }

        public void FullRecharge()
        {
            SetBatteryInternal(maxBattery);
        }

        /// <summary>
        /// Swap to a new battery pack (e.g., equipping better pack).
        /// Optionally refill to max or keep percent.
        /// </summary>
        public void SetMaxBattery(float newMax, bool refill = true, bool keepPercent = false)
        {
            newMax = Mathf.Max(1f, newMax);

            float percent = BatteryNormalized;
            maxBattery = newMax;

            if (keepPercent)
                currentBattery = maxBattery * percent;
            else if (refill)
                currentBattery = maxBattery;
            else
                currentBattery = Mathf.Min(currentBattery, maxBattery);

            SetBatteryInternal(currentBattery);
        }

        // ---------------- Internals ----------------

        private void DrainContinuous(float amount)
        {
            if (amount <= 0f) return;
            if (IsDepleted) return;

            float final = amount * drainMultiplier;
            lastDrainTime = Time.time;
            SetBatteryInternal(currentBattery - final);
        }

        private void SetBatteryInternal(float value)
        {
            bool wasDepleted = IsDepleted;

            currentBattery = Mathf.Clamp(value, 0f, maxBattery);
            IsDepleted = currentBattery <= 0f;

            // Low battery event
            if (!lowBatteryFired && BatteryNormalized <= lowBatteryThreshold && !IsDepleted)
            {
                lowBatteryFired = true;
                OnLowBattery?.Invoke();
            }
            if (BatteryNormalized > lowBatteryThreshold) lowBatteryFired = false;

            // Depleted/restored events
            if (!wasDepleted && IsDepleted)
                OnBatteryDepleted?.Invoke();
            else if (wasDepleted && !IsDepleted)
                OnBatteryRestored?.Invoke();

            BroadcastChanged();
        }

        private void BroadcastChanged()
        {
            OnBatteryChanged?.Invoke(currentBattery, maxBattery, BatteryNormalized);
        }

        private void OnValidate()
        {
            maxBattery = Mathf.Max(1f, maxBattery);
            currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);
            drainMultiplier = Mathf.Max(0f, drainMultiplier);
        }
    }
}
