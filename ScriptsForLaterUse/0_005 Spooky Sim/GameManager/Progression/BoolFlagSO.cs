// BoolFlagSO.cs
using NUnit.Framework;
using System;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Cherry/Conditions/Bool Flag", fileName = "NewBoolFlag")]
public class BoolFlagSO : ScriptableObject
{
    [Header("Defaults & Persistence")]
    [SerializeField] private bool initialValue = false;

    [Tooltip("Optional PlayerPrefs fallback for quick tests.")]
    [SerializeField] private bool usePlayerPrefs = false;

    [SerializeField] private string playerPrefsKey = "";

    [Header("Save/Load")]
    [Tooltip("Generated once. Do not edit after first save.")]
    [SerializeField] private string stableId = "";

    [Tooltip("Include this flag in the runtime registry.")]
    [SerializeField] private bool registerWithRegistry = true;

    public event Action<bool> OnChanged;

    bool _value;
    public bool Value => _value;
    public string StableId => string.IsNullOrEmpty(stableId) ? name : stableId;
    string PlayerPrefsKey => string.IsNullOrEmpty(playerPrefsKey) ? name : playerPrefsKey;

    void OnEnable()
    {
        if (string.IsNullOrEmpty(stableId))
        {
            stableId = Guid.NewGuid().ToString("N");
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        _value = initialValue;
        if (usePlayerPrefs && PlayerPrefs.HasKey(PlayerPrefsKey))
            _value = PlayerPrefs.GetInt(PlayerPrefsKey, 0) != 0;

        if (registerWithRegistry) FlagRegistry.Register(this);
    }

    void OnDisable()
    {
        if (registerWithRegistry) FlagRegistry.Unregister(this);
    }

    public void Set(bool v, bool fireEvents = true)
    {
        if (_value == v) return;
        _value = v;

        if (_value)
        {
            //SaveData.Current.mainData.progressionData.flagSOs.Add(StableId);
            bool existed = false;

            if(SaveData.Current.mainData.progressionData.flagSOs == null)
            {
                SaveData.Current.mainData.progressionData.flagSOs = new List<FlagsSaveData>();
            }

            for(int i = 0; i < SaveData.Current.mainData.progressionData.flagSOs.Count; i++)
            {

                if (SaveData.Current.mainData.progressionData.flagSOs[i].id == stableId)
                {
                    SaveData.Current.mainData.progressionData.flagSOs[i] = new FlagsSaveData { id = stableId, value = true };
                    existed = true;
                }
            }


            if (!existed)
            {
                SaveData.Current.mainData.progressionData.flagSOs.Add(new FlagsSaveData { id = stableId, value = true });
            }
        }

        if (usePlayerPrefs)
        {
            PlayerPrefs.SetInt(PlayerPrefsKey, v ? 1 : 0);
            PlayerPrefs.Save();
        }

        if (fireEvents) OnChanged?.Invoke(_value);
    }

    public void SetTrue(bool fireEvents = true) => Set(true, fireEvents);
    public void SetFalse(bool fireEvents = true) => Set(false, fireEvents);
}
