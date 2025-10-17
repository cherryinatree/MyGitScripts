using System;
using UnityEngine;

namespace LightingSystem
{
    /// <summary>
    /// Master controller that can broadcast commands to LightAgent subscribers.
    /// Call its public methods from UI Buttons, Timeline, or other scripts.
    /// </summary>
    [ExecuteAlways]
    public class MasterLightController : MonoBehaviour
    {
        // ---- Broadcast events (instance-scoped) ----
        // channel: -1 = all channels, otherwise exact channel match
        public event Action<int, bool> OnToggle;
        public event Action<int, Color> OnColor;
        public event Action<int, float> OnIntensity;
        public event Action<int, FlickerSettings> OnStartFlicker;
        public event Action<int> OnStopFlicker;
        public event Action<int, PatternSettings> OnPattern;

        [Header("Default Broadcast Channel (-1 = All)")]
        [Tooltip("When using the parameterless convenience methods, this channel is used.")]
        public int defaultChannel = -1;

        // ---------- Convenience: target ALL or a channel ----------
        public void ToggleAll(bool on) => ToggleChannel(-1, on);
        public void ToggleChannel(int channel, bool on) => OnToggle?.Invoke(channel, on);

        public void SetAllColor(Color color) => SetChannelColor(-1, color);
        public void SetChannelColor(int channel, Color color) => OnColor?.Invoke(channel, color);

        public void SetAllIntensity(float intensity) => SetChannelIntensity(-1, intensity);
        public void SetChannelIntensity(int channel, float intensity) => OnIntensity?.Invoke(channel, intensity);

        public void StartAllFlicker(FlickerSettings settings) => StartChannelFlicker(-1, settings);
        public void StartChannelFlicker(int channel, FlickerSettings settings) => OnStartFlicker?.Invoke(channel, settings);

        public void StopAllFlicker() => StopChannelFlicker(-1);
        public void StopChannelFlicker(int channel) => OnStopFlicker?.Invoke(channel);

        public void ApplyPatternToAll(PatternSettings settings) => ApplyPatternToChannel(-1, settings);
        public void ApplyPatternToChannel(int channel, PatternSettings settings) => OnPattern?.Invoke(channel, settings);

        // ---------- Parameterless convenience methods use defaultChannel ----------
        [ContextMenu("Demo/Turn ON (defaultChannel)")]
        public void DemoOn() => ToggleChannel(defaultChannel, true);

        [ContextMenu("Demo/Turn OFF (defaultChannel)")]
        public void DemoOff() => ToggleChannel(defaultChannel, false);

        [ContextMenu("Demo/Red Color (defaultChannel)")]
        public void DemoRed() => SetChannelColor(defaultChannel, Color.red);

        [ContextMenu("Demo/Pulse Pattern (defaultChannel)")]
        public void DemoPulse()
        {
            var p = PatternSettings.MakePulse(speed: 1.0f, baseIntensity: 1.0f, amplitude: 0.5f);
            ApplyPatternToChannel(defaultChannel, p);
        }

        [ContextMenu("Demo/Stop Flicker & Pattern (defaultChannel)")]
        public void DemoStopAllFx()
        {
            OnStopFlicker?.Invoke(defaultChannel);
            // A "None" pattern effectively clears the pattern on agents
            ApplyPatternToChannel(defaultChannel, PatternSettings.None());
        }
    }

    // ----- Shared data containers -----

    [Serializable]
    public struct FlickerSettings
    {
        [Tooltip("Multipliers applied to the agent's BASE intensity while flickering.")]
        public float minIntensityMultiplier;   // e.g., 0.5
        public float maxIntensityMultiplier;   // e.g., 1.2

        [Tooltip("Random wait between flicker updates (seconds).")]
        public float minInterval;              // e.g., 0.02
        public float maxInterval;              // e.g., 0.15

        [Tooltip("If true: hard on/off style flicker using 0 or 1 multipliers.")]
        public bool hardOnOff;

        [Tooltip("Total duration in seconds; <= 0 = infinite until StopFlicker.")]
        public float duration;

        public static FlickerSettings Soft(float duration = 2f) => new FlickerSettings
        {
            minIntensityMultiplier = 0.7f,
            maxIntensityMultiplier = 1.1f,
            minInterval = 0.03f,
            maxInterval = 0.12f,
            hardOnOff = false,
            duration = duration
        };

        public static FlickerSettings Strobe(float duration = 1.5f) => new FlickerSettings
        {
            minIntensityMultiplier = 0f,
            maxIntensityMultiplier = 1f,
            minInterval = 0.02f,
            maxInterval = 0.06f,
            hardOnOff = true,
            duration = duration
        };
    }

    public enum PatternMode { None, Pulse, Strobe, ColorCycle }

    [Serializable]
    public struct PatternSettings
    {
        public PatternMode mode;

        [Tooltip("Base intensity the pattern is built on (agents multiply against their original base).")]
        public float baseIntensity;        // e.g., 1.0

        [Tooltip("Pattern speed in cycles per second or flashes per second depending on mode.")]
        public float speed;                // e.g., 1.0

        [Tooltip("Amplitude for Pulse pattern (0..1 recommended).")]
        public float amplitude;            // Pulse only

        [Tooltip("Used for ColorCycle; if empty a default rainbow is used.")]
        public Gradient colorGradient;     // ColorCycle

        [Tooltip("Each agent adds (index * phaseOffsetPerAgent) to its phase.")]
        public float phaseOffsetPerAgent;  // e.g., 0.15f

        public static PatternSettings None() => new PatternSettings { mode = PatternMode.None, baseIntensity = 1f, speed = 0f, amplitude = 0f, phaseOffsetPerAgent = 0f };

        public static PatternSettings MakePulse(float speed, float baseIntensity = 1f, float amplitude = 0.5f) => new PatternSettings
        {
            mode = PatternMode.Pulse,
            baseIntensity = baseIntensity,
            speed = Mathf.Max(0f, speed),
            amplitude = Mathf.Clamp01(amplitude),
            phaseOffsetPerAgent = 0.15f
        };

        public static PatternSettings MakeStrobe(float speed, float baseIntensity = 1f) => new PatternSettings
        {
            mode = PatternMode.Strobe,
            baseIntensity = baseIntensity,
            speed = Mathf.Max(0.1f, speed),
            amplitude = 1f,
            phaseOffsetPerAgent = 0f
        };

        public static PatternSettings MakeColorCycle(float speed, Gradient gradient = null, float baseIntensity = 1f) => new PatternSettings
        {
            mode = PatternMode.ColorCycle,
            baseIntensity = baseIntensity,
            speed = Mathf.Max(0.1f, speed),
            amplitude = 1f,
            phaseOffsetPerAgent = 0.1f,
            colorGradient = gradient
        };
    }
}

