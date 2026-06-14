using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

[DisallowMultipleComponent]
public class CinemachinePrioritySwitcher : MonoBehaviour
{
    [Serializable]
    public class Step
    {
        public CinemachineVirtualCameraBase vcam;
        [Min(0f)] public float preDelay = 0f;   // wait BEFORE switching to this camera
        [Min(0f)] public float holdSeconds = 1f; // wait AFTER switching to this camera
    }

    [Serializable]
    public class VCamEvent : UnityEngine.Events.UnityEvent<CinemachineVirtualCameraBase> { }

    [Header("Managed Cameras")]
    [Tooltip("Cameras this script will manage. You can also auto-find from children.")]
    public List<CinemachineVirtualCameraBase> managed = new();

    [Tooltip("If true, managed list is auto-filled from children on Awake.")]
    public bool autoFindFromChildren = false;

    [Tooltip("Include inactive objects when auto-finding.")]
    public bool includeInactiveWhenFinding = true;

    [Header("Priority Boosting")]
    [Tooltip("Active camera gets basePriority + activeBoost.")]
    public int activeBoost = 50;

    [Tooltip("Inactive managed cameras get basePriority + inactiveBoost.")]
    public int inactiveBoost = 0;

    [Header("Start Options")]
    public bool switchOnStart = false;
    public CinemachineVirtualCameraBase startCamera;
    public int startIndex = 0;

    public bool switchAfterDelay = false;
    [Min(0f)] public float delaySeconds = 1f;
    public CinemachineVirtualCameraBase delayedCamera;
    public int delayedIndex = 0;

    [Header("Sequence Options")]
    public bool playSequenceOnStart = false;
    public bool loopSequence = false;
    public List<Step> sequence = new();

    [Header("Auto Cycle Options")]
    public bool autoCycleOnStart = false;
    public bool loopCycle = true;
    [Min(0.01f)] public float cycleInterval = 3f;

    [Header("Events")]
    public VCamEvent onSwitched;

    private readonly Dictionary<CinemachineVirtualCameraBase, int> _basePriority = new();
    private Coroutine _sequenceCo;
    private Coroutine _cycleCo;

    private void Awake()
    {
        if (autoFindFromChildren)
        {
            managed.Clear();
            var found = GetComponentsInChildren<CinemachineVirtualCameraBase>(includeInactiveWhenFinding);
            managed.AddRange(found);
        }

        RebuildBasePriorityCache();
    }

    private void Start()
    {
        // Optional immediate switch
        if (switchOnStart)
            SwitchTo(ResolveCamera(startCamera, startIndex));

        // Optional delayed switch
        if (switchAfterDelay)
            StartCoroutine(DelayedSwitchRoutine());

        // Optional sequence / cycle
        if (playSequenceOnStart)
            StartSequence();

        if (autoCycleOnStart)
            StartCycle();
    }
    private IEnumerator DelayedSwitchRoutine()
    {
        if (delaySeconds > 0f)
            yield return new WaitForSeconds(delaySeconds);

        SwitchTo(ResolveCamera(delayedCamera, delayedIndex));
    }

    public void RebuildBasePriorityCache()
    {
        _basePriority.Clear();
        for (int i = 0; i < managed.Count; i++)
        {
            var v = managed[i];
            if (!v) continue;
            if (!_basePriority.ContainsKey(v))
                _basePriority.Add(v, v.Priority);
        }
    }

    private CinemachineVirtualCameraBase ResolveCamera(CinemachineVirtualCameraBase cam, int index)
    {
        if (cam) return cam;
        if (index >= 0 && index < managed.Count) return managed[index];
        return null;
    }

    public void SwitchToIndex(int index) => SwitchTo(ResolveCamera(null, index));

    public void SwitchTo(CinemachineVirtualCameraBase target)
    {
        if (!target)
        {
            Debug.LogWarning($"{name}: SwitchTo called with null target.");
            return;
        }

        // Ensure target is cached (even if not in managed list)
        CacheIfNeeded(target);

        // Apply inactive boost to all managed cameras
        for (int i = 0; i < managed.Count; i++)
        {
            var v = managed[i];
            if (!v) continue;

            CacheIfNeeded(v);
            v.Priority = _basePriority[v] + inactiveBoost;
        }

        // Apply active boost to target
        target.Priority = _basePriority[target] + activeBoost;

        onSwitched?.Invoke(target);
    }

    private void CacheIfNeeded(CinemachineVirtualCameraBase vcam)
    {
        if (!vcam) return;
        if (!_basePriority.ContainsKey(vcam))
            _basePriority[vcam] = vcam.Priority;
    }

    public void StopAllModes()
    {
        StopSequence();
        StopCycle();
    }

    public void StartSequence()
    {
        StopSequence();
        _sequenceCo = StartCoroutine(SequenceRoutine());
    }

    public void StopSequence()
    {
        if (_sequenceCo != null)
        {
            StopCoroutine(_sequenceCo);
            _sequenceCo = null;
        }
    }

    private IEnumerator SequenceRoutine()
    {
        if (sequence == null || sequence.Count == 0)
        {
            Debug.LogWarning($"{name}: Sequence is empty.");
            yield break;
        }

        do
        {
            for (int i = 0; i < sequence.Count; i++)
            {
                var step = sequence[i];
                if (step == null || !step.vcam) continue;

                if (step.preDelay > 0f) yield return new WaitForSeconds(step.preDelay);
                SwitchTo(step.vcam);
                if (step.holdSeconds > 0f) yield return new WaitForSeconds(step.holdSeconds);
            }
        }
        while (loopSequence);

        _sequenceCo = null;
    }

    public void StartCycle()
    {
        StopCycle();
        _cycleCo = StartCoroutine(CycleRoutine());
    }

    public void StopCycle()
    {
        if (_cycleCo != null)
        {
            StopCoroutine(_cycleCo);
            _cycleCo = null;
        }
    }

    private IEnumerator CycleRoutine()
    {
        if (managed == null || managed.Count == 0)
        {
            Debug.LogWarning($"{name}: Managed list is empty. Cannot cycle.");
            yield break;
        }

        int idx = 0;

        while (true)
        {
            // Find next non-null
            int attempts = 0;
            CinemachineVirtualCameraBase next = null;
            while (attempts < managed.Count)
            {
                if (managed[idx])
                {
                    next = managed[idx];
                    break;
                }
                idx = (idx + 1) % managed.Count;
                attempts++;
            }

            if (next) SwitchTo(next);

            yield return new WaitForSeconds(cycleInterval);

            idx++;
            if (idx >= managed.Count)
            {
                if (!loopCycle) break;
                idx = 0;
            }
        }

        _cycleCo = null;
    }

    // Convenience: next/previous for UI buttons etc.
    public void Next()
    {
        if (managed == null || managed.Count == 0) return;
        int current = GetHighestPriorityManagedIndex();
        int next = (current + 1) % managed.Count;
        SwitchToIndex(next);
    }

    public void Previous()
    {
        if (managed == null || managed.Count == 0) return;
        int current = GetHighestPriorityManagedIndex();
        int prev = (current - 1 + managed.Count) % managed.Count;
        SwitchToIndex(prev);
    }

    private int GetHighestPriorityManagedIndex()
    {
        int best = 0;
        int bestPri = int.MinValue;
        for (int i = 0; i < managed.Count; i++)
        {
            var v = managed[i];
            if (!v) continue;
            if (v.Priority > bestPri)
            {
                bestPri = v.Priority;
                best = i;
            }
        }
        return best;
    }
}
