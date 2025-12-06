using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoreSurfaceSaveController : MonoBehaviour
{
    [SerializeField] private FinishMaterialLibrary library;

    [Header("Auto")]
    [SerializeField] private bool loadOnStart = true;
    [SerializeField] private bool saveOnApplicationQuit = true;

    private string PlayerPrefsKey => $"StoreSurfaceCustomization::{SceneManager.GetActiveScene().name}";

    private void Awake()
    {
        if (library) library.BuildLookup();
        if (loadOnStart) LoadFromPlayerPrefs();
    }

    private void OnApplicationQuit()
    {
        if (saveOnApplicationQuit) SaveToPlayerPrefs();
    }

    public void SaveToPlayerPrefs()
    {
        var data = CaptureAll();
        var json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(PlayerPrefsKey, json);
        PlayerPrefs.Save();
    }

    public void LoadFromPlayerPrefs()
    {
        if (!PlayerPrefs.HasKey(PlayerPrefsKey)) return;

        var json = PlayerPrefs.GetString(PlayerPrefsKey, "");
        if (string.IsNullOrWhiteSpace(json)) return;

        var data = JsonUtility.FromJson<WorldCustomizationSaveData>(json);
        ApplyAll(data);
    }

    public WorldCustomizationSaveData CaptureAll()
    {
        var data = new WorldCustomizationSaveData();

        foreach (var s in StoreSurfaceRegistry.All)
        {
            if (!s) continue;
            var st = s.CaptureState();
            if (string.IsNullOrWhiteSpace(st.finishId)) continue; // skip untouched
            data.surfaces.Add(st);
        }

        return data;
    }

    public void ApplyAll(WorldCustomizationSaveData data)
    {
        if (data == null || data.surfaces == null) return;
        if (!library) return;

        var map = new Dictionary<string, StoreSurfaceState>(StringComparer.Ordinal);
        foreach (var st in data.surfaces)
        {
            if (!string.IsNullOrWhiteSpace(st.key))
                map[st.key] = st;
        }

        foreach (var s in StoreSurfaceRegistry.All)
        {
            if (!s) continue;
            if (map.TryGetValue(s.SaveKey, out var st))
                s.RestoreState(st, library);
        }
    }
}

[Serializable]
public class WorldCustomizationSaveData
{
    public List<StoreSurfaceState> surfaces = new();
}

[Serializable]
public struct StoreSurfaceState
{
    public string key;      // scene|surfaceUniqueId
    public string finishId; // FinishMaterialEntry.id
    public Color tint;
    public Vector2 tiling;
}
