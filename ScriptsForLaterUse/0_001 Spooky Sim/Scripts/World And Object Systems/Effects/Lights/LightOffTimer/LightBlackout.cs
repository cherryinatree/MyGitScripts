using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turns selected lights OFF for a duration, then restores their previous state.
/// Works with realtime/mixed lights (baked lights won't change at runtime).
/// </summary>
public class LightBlackout : MonoBehaviour
{
    public enum TargetMode { AllSceneLights, ChildrenOnly, ManualList }

    [Header("Targets")]
    [Tooltip("What lights should be affected?")]
    public TargetMode targetMode = TargetMode.AllSceneLights;

    [Tooltip("If ManualList, populate this with the lights to control.")]
    public List<Light> manualLights = new List<Light>();

    [Tooltip("If ChildrenOnly, finds Light components in this transform's hierarchy.")]
    public Transform rootForChildren;

    [Header("Behavior")]
    [Tooltip("If a blackout is triggered while one is already running, restart the timer and keep lights off.")]
    public bool restartIfRunning = true;

    [Tooltip("Optional: a quick test trigger key in Play Mode.")]
    public KeyCode testKey = KeyCode.None;

    public float BlackOutTime = 3f;

    // ----- internal -----
    private struct LightState
    {
        public Light light;
        public bool wasEnabled;
        public float intensity;
    }

    private readonly List<LightState> _cachedStates = new();
    private Coroutine _running;

    /// <summary>
    /// Call this from other scripts, UnityEvents, or the Inspector via Button attributes (if you use Odin etc.).
    /// </summary>
    public void TriggerBlackout(float seconds)
    {
        if (_running != null)
        {
            if (restartIfRunning)
            {
                StopCoroutine(_running);
                // leave lights off while restarting with fresh timer
            }
            else
            {
                // Already running; ignore.
                return;
            }
        }

        _running = StartCoroutine(BlackoutRoutine(Mathf.Max(0f, seconds)));
    }

    private void Update()
    {
       /* if (testKey != KeyCode.None && Input.GetKeyDown(testKey))
        {
            TriggerBlackout(BlackOutTime); // test: 3 seconds
        }*/
    }

    private IEnumerator BlackoutRoutine(float seconds)
    {

        Debug.Log("Blackout");
        // Gather targets fresh each time (handles lights added/removed at runtime)
        var targets = GatherTargets();

        // Cache current states
        _cachedStates.Clear();
        foreach (var l in targets)
        {
            if (l == null) continue;
            _cachedStates.Add(new LightState
            {
                light = l,
                wasEnabled = l.enabled,
                intensity = l.intensity
            });
        }

        // Turn all off (disable is cheapest + also stops shadows)
        foreach (var s in _cachedStates)
        {
            if (s.light == null) continue;
            s.light.enabled = false;
        }

        // Wait requested time
        if (seconds > 0f)
            yield return new WaitForSeconds(seconds);

        // Restore
        foreach (var s in _cachedStates)
        {
            if (s.light == null) continue;
            s.light.enabled = s.wasEnabled;
            // If you prefer fading back in, comment the line above and set intensity instead.
            s.light.intensity = s.intensity; // (no-op if using enabled toggle)
        }

        _running = null;
    }

    private List<Light> GatherTargets()
    {
        var list = new List<Light>();

        switch (targetMode)
        {
            case TargetMode.AllSceneLights:
                // Finds all active and inactive Light components in the scene
                list.AddRange(Resources.FindObjectsOfTypeAll<Light>());
                // Filter out assets/prefabs not in scene
                list.RemoveAll(l => l == null || !IsInScene(l));
                break;

            case TargetMode.ChildrenOnly:
                if (rootForChildren == null) rootForChildren = transform;
                rootForChildren.GetComponentsInChildren(true, list);
                break;

            case TargetMode.ManualList:
                foreach (var l in manualLights)
                    if (l != null) list.Add(l);
                break;
        }

        // Keep only realtime or mixed (baked won't change at runtime)
        list.RemoveAll(l => l.lightmapBakeType == UnityEngine.LightmapBakeType.Baked);

        return list;
    }

    private bool IsInScene(Object obj)
    {
        // Exclude prefab assets; include objects from loaded scenes
        var go = (obj as Component)?.gameObject;
        if (go == null) return false;
        return go.scene.IsValid() && go.scene.isLoaded;
    }
}
