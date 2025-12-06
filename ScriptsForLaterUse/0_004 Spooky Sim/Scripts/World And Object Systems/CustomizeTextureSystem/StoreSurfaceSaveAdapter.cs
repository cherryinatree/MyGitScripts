using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoreSurfaceSaveAdapter : MonoBehaviour
{
    [SerializeField] private FinishMaterialLibrary library;

    [Header("Auto")]
    [SerializeField] private bool loadOnStart = true;

    private void Awake()
    {
        if (library) library.BuildLookup();
    }

    private void Start()
    {
        // Start() runs after all surfaces' Awake() (so registry is populated)
        if (loadOnStart) LoadFromSaveData();
    }

    public void SaveToSaveData()
    {
        if (SaveData.Current == null || SaveData.Current.mainData == null) return;

        var store = SaveData.Current.mainData.storeData;
        if (store == null) return;

        if (store.surfaceCustomizations == null)
            store.surfaceCustomizations = new List<StoreSurfaceCustomizationData>();

        store.surfaceCustomizations.Clear();

        string scene = SceneManager.GetActiveScene().name;

        foreach (var s in StoreSurfaceRegistry.All)
        {
            if (!s) continue;

            var c = s.CaptureCustomization();

            // Only save surfaces in the current scene (common pattern)
            if (c.sceneName != scene) continue;

            // Skip untouched
            if (string.IsNullOrWhiteSpace(c.finishId)) continue;

            store.surfaceCustomizations.Add(c);
        }

        // IMPORTANT: This only writes into SaveData memory.
        // Call your existing "write to disk" save method after this.
    }

    public void LoadFromSaveData()
    {
        if (SaveData.Current == null || SaveData.Current.mainData == null) return;

        var store = SaveData.Current.mainData.storeData;
        if (store == null) return;

        var list = store.surfaceCustomizations;
        if (list == null || list.Count == 0) return;
        if (!library) return;

        string scene = SceneManager.GetActiveScene().name;

        // Build lookup surfaceId -> customization for this scene
        var map = new Dictionary<string, StoreSurfaceCustomizationData>(System.StringComparer.Ordinal);
        foreach (var c in list)
        {
            if (c.sceneName != scene) continue;
            if (!string.IsNullOrWhiteSpace(c.surfaceId))
                map[c.surfaceId] = c;
        }

        foreach (var s in StoreSurfaceRegistry.All)
        {
            if (!s) continue;
            if (map.TryGetValue(s.UniqueId, out var c))
                s.RestoreCustomization(c, library);
        }
    }
}
