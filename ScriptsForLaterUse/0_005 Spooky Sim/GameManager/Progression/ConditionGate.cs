// ConditionGate.cs 
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("Cherry/Conditions/Condition Gate")]
public class ConditionGate : MonoBehaviour
{
    public enum Mode { All, Any }

    public enum GameObjectAction
    {
        DoNothing,
        Enable,
        Disable
    }

    [Serializable]
    public class GameObjectConditionRule
    {
        public GameObject target;

        [Header("What happens when the condition is MET?")]
        public GameObjectAction whenMet = GameObjectAction.DoNothing;

        [Header("What happens when the condition is UNMET?")]
        public GameObjectAction whenUnmet = GameObjectAction.DoNothing;
    }

    [Header("Logic")]
    [SerializeField] private Mode mode = Mode.All;

    [Tooltip("If no flags are assigned, should the condition be considered met?")]
    [SerializeField] private bool treatEmptyAsMet = false;

    [SerializeField] private List<BoolFlagSO> flags = new();

    [Header("Targets to toggle when MET")]
    [Tooltip("Behaviours to enable when condition is MET, disable when UNMET.")]
    [SerializeField] private List<Behaviour> enableWhenMet = new();

    [Tooltip("GameObjects to activate when condition is MET, deactivate when UNMET.")]
    [SerializeField] private List<GameObject> activateWhenMet = new();

    [Header("Targets to toggle when UNMET (optional)")]
    [SerializeField] private List<Behaviour> enableWhenUnmet = new();

    [SerializeField] private List<GameObject> activateWhenUnmet = new();

    [Header("Custom GameObject Rules")]
    [Tooltip("Flexible rules for enabling/disabling GameObjects when the condition is met or unmet.")]
    [SerializeField] private List<GameObjectConditionRule> gameObjectRules = new();

    [Header("Events")]
    public UnityEvent OnMet;
    public UnityEvent OnUnmet;

    [Header("Options")]
    [SerializeField] private bool evaluateOnAwake = true;

    [Header("Save Sync (SaveData.Current.mainData.progression.flagSOs)")]
    [Tooltip("Pull saved values into assigned flags on Awake.")]
    [SerializeField] private bool pullFromSaveOnAwake = true;

    [Tooltip("Also pull on OnEnable (useful if objects are pooled/reenabled).")]
    [SerializeField] private bool pullFromSaveOnEnable = false;

    [Tooltip("Fire each flag's OnChanged when applying saved values.")]
    [SerializeField] private bool fireEventsWhenPulled = false;

    private bool _lastState;
    private bool _subscribed;

    void Start()
    {
        if (pullFromSaveOnAwake)
            RefreshFromSave(fireEventsWhenPulled);

        if (evaluateOnAwake)
            EvaluateAndApply();
    }

    void OnEnable()
    {
        Subscribe(true);

        if (pullFromSaveOnEnable)
            RefreshFromSave(fireEventsWhenPulled);

        EvaluateAndApply();
    }

    void OnDisable()
    {
        Subscribe(false);
    }

    void Subscribe(bool on)
    {
        if (flags == null)
            return;

        if (on)
        {
            if (_subscribed)
                return;

            foreach (var f in flags)
            {
                if (!f) continue;
                f.OnChanged += HandleChanged;
            }

            _subscribed = true;
        }
        else
        {
            if (!_subscribed)
                return;

            foreach (var f in flags)
            {
                if (!f) continue;
                f.OnChanged -= HandleChanged;
            }

            _subscribed = false;
        }
    }

    void HandleChanged(bool _) => EvaluateAndApply();

    bool IsMet()
    {
        if (flags == null || flags.Count == 0)
            return treatEmptyAsMet;

        bool anyTrue = false;

        foreach (var f in flags)
        {
            if (!f) continue;

            if (f.Value)
            {
                anyTrue = true;
            }
            else if (mode == Mode.All)
            {
                return false;
            }
        }

        return mode == Mode.All ? true : anyTrue;
    }

    public void EvaluateAndApply()
    {
        bool met = IsMet();
        _lastState = met;

        Debug.Log($"{name}: ConditionGate evaluated as {(met ? "MET" : "UNMET")}.");

        foreach (var b in enableWhenMet)
        {
            if (b) b.enabled = met;
        }

        foreach (var go in activateWhenMet)
        {
            if (go) go.SetActive(met);
        }

        foreach (var b in enableWhenUnmet)
        {
            if (b) b.enabled = !met;
        }

        foreach (var go in activateWhenUnmet)
        {
            if (go) go.SetActive(!met);
        }

        ApplyGameObjectRules(met);

        if (met)
            OnMet?.Invoke();
        else
            OnUnmet?.Invoke();
    }

    private void ApplyGameObjectRules(bool met)
    {
        if (gameObjectRules == null)
            return;

        foreach (var rule in gameObjectRules)
        {
            if (rule == null || rule.target == null)
                continue;

            GameObjectAction action = met ? rule.whenMet : rule.whenUnmet;

            switch (action)
            {
                case GameObjectAction.Enable:
                    rule.target.SetActive(true);
                    break;

                case GameObjectAction.Disable:
                    rule.target.SetActive(false);
                    break;

                case GameObjectAction.DoNothing:
                    break;
            }
        }
    }

    /// <summary>
    /// Pulls values for this gate's assigned flags from
    /// SaveData.Current.mainData.progression.flagSOs and applies them.
    /// </summary>
    [ContextMenu("Refresh From Save")]
    public void RefreshFromSave() => RefreshFromSave(fireEventsWhenPulled);

    public void RefreshFromSave(bool fireEvents)
    {
        var prog = SaveData.Current?.mainData?.progressionData;

        if (prog == null)
        {
            Debug.LogWarning($"{name}: ConditionGate couldn't find SaveData.Current.mainData.progression.");
            return;
        }

        var list = prog.flagSOs;

        Debug.Log("ConditionGate.RefreshFromSave: found " +
                  (list != null ? list.Count.ToString() : "null") +
                  " saved flags.");

        if (list == null || list.Count == 0 || flags == null || flags.Count == 0)
            return;

        foreach (var flag in flags)
        {
            if (!flag) continue;

            string id = flag.StableId;

            if (string.IsNullOrEmpty(id))
                continue;

            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];

                if (entry.id == id)
                {
                    flag.Set(entry.value, fireEvents);
                    break;
                }
            }
        }

        EvaluateAndApply();
    }
}