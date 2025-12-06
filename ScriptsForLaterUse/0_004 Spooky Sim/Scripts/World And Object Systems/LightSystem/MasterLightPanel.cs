// MasterLightPanel.cs
// Put this on any GameObject. Tweak values in the Inspector to control lights via MasterLightController.
// Works in Play Mode AND Edit Mode (with ExecuteAlways on Master/Agents).

using UnityEngine;
using LightingSystem;

[ExecuteAlways]
public class MasterLightPanel : MonoBehaviour
{
    [Header("Master")]
    [Tooltip("If not assigned, this will auto-find one in the scene.")]
    public MasterLightController master;

    [Header("Targeting")]
    [Tooltip("-1 = all channels; otherwise exact channel match.")]
    public int channel = -1;

    [Header("Auto-Apply")]
    [Tooltip("If ON, changes you make in the inspector are applied immediately (in edit or play).")]
    public bool autoApply = true;

    // --- TOGGLE ---
    [Header("Toggle")]
    public bool setToggle;
    [Tooltip("Applies only if 'setToggle' is checked.")]
    public bool turnOn = true;

    // --- COLOR ---
    [Header("Color")]
    public bool setColor;
    [Tooltip("Applies only if 'setColor' is checked.")]
    public Color color = Color.white;

    // --- INTENSITY ---
    [Header("Intensity")]
    public bool setIntensity;
    [Min(0f)]
    [Tooltip("Applies only if 'setIntensity' is checked.")]
    public float intensity = 1f;

    // --- FLICKER ---
    [Header("Flicker")]
    [Tooltip("Start a flicker (play mode animation by default).")]
    public bool startFlicker;
    public FlickerSettings flicker = FlickerSettings.Soft(2f);

    [Tooltip("Stop flicker on the targeted lights.")]
    public bool stopFlicker;

    // --- PATTERN ---
    [Header("Pattern")]
    public bool setPattern;
    public PatternSettings pattern = PatternSettings.None();

    [Tooltip("Clear any pattern on the targeted lights.")]
    public bool clearPattern;

    // Manual buttons in context menu (handy if autoApply is off)
    [ContextMenu("Apply/Toggle")]
    public void ApplyToggle() { if (EnsureMaster()) master.ToggleChannel(channel, turnOn); }

    [ContextMenu("Apply/Color")]
    public void ApplyColor() { if (EnsureMaster()) master.SetChannelColor(channel, color); }

    [ContextMenu("Apply/Intensity")]
    public void ApplyIntensity() { if (EnsureMaster()) master.SetChannelIntensity(channel, intensity); }

    [ContextMenu("Apply/Start Flicker")]
    public void ApplyStartFlicker() { if (EnsureMaster()) master.StartChannelFlicker(channel, flicker); }

    [ContextMenu("Apply/Stop Flicker")]
    public void ApplyStopFlicker() { if (EnsureMaster()) master.StopChannelFlicker(channel); }

    [ContextMenu("Apply/Pattern")]
    public void ApplyPattern() { if (EnsureMaster()) master.ApplyPatternToChannel(channel, pattern); }

    [ContextMenu("Apply/Clear Pattern")]
    public void ApplyClearPattern() { if (EnsureMaster()) master.ApplyPatternToChannel(channel, PatternSettings.None()); }

    // Auto-find master if missing
    private void Awake() { TryFindMaster(); }
    private void OnEnable() { TryFindMaster(); }

    private void TryFindMaster()
    {
        if (master == null)
            master = FindFirstObjectByType<MasterLightController>();
    }

    // Auto-apply whenever you tweak values in the inspector
    private void OnValidate()
    {
        if (!autoApply) return;
        if (!EnsureMaster()) return;

        if (setToggle) master.ToggleChannel(channel, turnOn);
        if (setColor) master.SetChannelColor(channel, color);
        if (setIntensity) master.SetChannelIntensity(channel, intensity);
        if (startFlicker) master.StartChannelFlicker(channel, flicker);
        if (stopFlicker) master.StopChannelFlicker(channel);
        if (setPattern) master.ApplyPatternToChannel(channel, pattern);
        if (clearPattern) master.ApplyPatternToChannel(channel, PatternSettings.None());
    }

    private bool EnsureMaster()
    {
        if (master != null) return true;
        TryFindMaster();
        return master != null;
    }
}
