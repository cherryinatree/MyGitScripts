using System.Collections.Generic;
using UnityEngine;

namespace Cherry.Combat
{
    /// <summary>
    /// Attach to any equipment that uses player battery.
    /// Supports:
    /// - Instant costs (TryUseOnce)
    /// - Multiple simultaneous continuous drains via "channels"
    /// - Safe against double-start/double-stop
    /// 
    /// NOTE: Multiple BatteryConsumers on different items are fine.
    /// PlayerBattery sums all BeginDrain calls globally.
    /// </summary>
    public class BatteryConsumer : MonoBehaviour
    {
        [SerializeField] private PlayerBattery battery;

        [Header("Default Costs (optional)")]
        [Tooltip("Battery per second while default drain is active.")]
        public float defaultDrainPerSecond = 5f;

        [Tooltip("Battery cost per single use (shot/click).")]
        public float costPerUse = 10f;

        // Local active drains for THIS consumer (channel -> rate).
        private readonly Dictionary<string, float> activeChannels = new();

        private void Awake()
        {
            if (!battery)
                battery = FindFirstObjectByType<PlayerBattery>();
        }

        public bool CanUse() => battery && !battery.IsDepleted;

        /// <summary>
        /// Instant battery cost (uses costPerUse). Returns false if empty.
        /// </summary>
        public bool TryUseOnce()
        {
            if (!battery) return false;
            return battery.TryConsume(costPerUse);
        }

        /// <summary>
        /// Instant cost with an explicit amount.
        /// </summary>
        public bool TryUseOnce(float cost)
        {
            if (!battery) return false;
            return battery.TryConsume(cost);
        }

        /// <summary>
        /// Start a continuous drain channel for this item.
        /// If already draining that channel, it won't double add.
        /// If you pass a different rate for an existing channel, it updates it safely.
        /// </summary>
        public void StartDrain(string channel, float drainPerSecond)
        {
            if (!battery) return;
            if (string.IsNullOrEmpty(channel)) channel = "default";

            drainPerSecond = Mathf.Max(0f, drainPerSecond);
            if (drainPerSecond <= 0f) return;

            // already active?
            if (activeChannels.TryGetValue(channel, out float existing))
            {
                // same rate -> no-op
                if (Mathf.Approximately(existing, drainPerSecond)) return;

                // rate changed -> remove old, add new
                battery.EndDrain(existing);
                activeChannels[channel] = drainPerSecond;
                battery.BeginDrain(drainPerSecond);
                return;
            }

            // new channel
            activeChannels[channel] = drainPerSecond;
            battery.BeginDrain(drainPerSecond);
        }

        /// <summary>
        /// Stop a continuous drain channel for this item.
        /// Safe if called when not active.
        /// </summary>
        public void StopDrain(string channel)
        {
            if (!battery) return;
            if (string.IsNullOrEmpty(channel)) channel = "default";

            if (!activeChannels.TryGetValue(channel, out float rate)) return;

            activeChannels.Remove(channel);
            battery.EndDrain(rate);
        }

        /// <summary>
        /// Stop all channels controlled by this item.
        /// </summary>
        public void StopAllDrains()
        {
            if (!battery) { activeChannels.Clear(); return; }

            foreach (var kvp in activeChannels)
                battery.EndDrain(kvp.Value);

            activeChannels.Clear();
        }

        /// <summary>
        /// Backwards-compatible convenience: toggles the default channel.
        /// </summary>
        public void SetDraining(bool active)
        {
            if (active) StartDrain("default", defaultDrainPerSecond);
            else StopDrain("default");
        }

        /// <summary>
        /// How much THIS item is draining right now.
        /// </summary>
        public float LocalDrainPerSecond
        {
            get
            {
                float sum = 0f;
                foreach (var r in activeChannels.Values) sum += r;
                return sum;
            }
        }

        private void OnDisable()
        {
            // If the item gets disabled while draining, clean up safely.
            StopAllDrains();
        }
    }
}
