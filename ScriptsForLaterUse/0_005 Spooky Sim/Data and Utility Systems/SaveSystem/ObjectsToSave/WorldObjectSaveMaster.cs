// WorldObjectSaveMaster.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class WorldObjectSaveMaster : MonoBehaviour
{
    public static WorldObjectSaveMaster Instance { get; private set; }

    [Header("Loading/Spawning")]
    [Tooltip("Optional parent for spawned objects when restoring.")]
    [SerializeField] private Transform spawnRoot;

    [Tooltip("Destroy existing registered SaveableObjects before restore.")]
    [SerializeField] private bool clearExistingOnRestore = true;

    private readonly Dictionary<string, SaveableObject> _byId = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Duplicate WorldObjectSaveMaster found. Destroying this one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // -------- Registry --------
    public void Register(SaveableObject s)
    {
        if (s == null) return;
        var id = s.ObjectId;
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning($"{s.name}: SaveableObject has empty id; assigning new one.");
#if UNITY_EDITOR
            // in-editor safety: assign and mark dirty
            var so = s.GetComponent<SaveableObject>();
            var field = typeof(SaveableObject).GetField("objectId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(so, Guid.NewGuid().ToString("N"));
            UnityEditor.EditorUtility.SetDirty(so);
#else
            return;
#endif
        }

        _byId[s.ObjectId] = s;
    }

    public void Unregister(SaveableObject s)
    {
        if (s == null) return;
        if (!string.IsNullOrWhiteSpace(s.ObjectId))
            _byId.Remove(s.ObjectId);
    }

    /// <summary>Called when an object moves/rotates or otherwise changes.</summary>
    public void NotifyObjectChanged(SaveableObject s)
    {
        // No-op here; your save system can poll BuildSnapshot/BuildSaveJson on demand,
        // or you can set a 'dirty' flag if you want autosave.
        _worldDirty = true;
    }

    private bool _worldDirty;

    // -------- Build Save --------
    public StoreSave BuildSnapshot()
    {
        var save = new StoreSave();
        foreach (var kv in _byId)
        {
            var s = kv.Value;
            if (s == null) continue;
            save.objects.Add(s.Capture());
        }
        return save;
    }

    public string BuildSaveJson()
    {
        var payload = BuildSnapshot();
        return JsonUtility.ToJson(payload, prettyPrint: false);
    }

    public static StoreSave ParseSaveJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new StoreSave();
        return JsonUtility.FromJson<StoreSave>(json);
    }

    // -------- Restore --------
    public void RestoreFromSave(StoreSave save)
    {
        if (save == null) return;

        if (clearExistingOnRestore)
        {
            // Destroy all current registered objects (avoid destroying the master!)
            var toKill = new List<GameObject>();
            foreach (var kv in _byId)
            {
                if (kv.Value != null) toKill.Add(kv.Value.gameObject);
            }
            _byId.Clear();
            foreach (var go in toKill)
            {
                if (go != null) Destroy(go);
            }
        }

        foreach (var rec in save.objects)
        {
            if (string.IsNullOrWhiteSpace(rec.prefabPath))
            {
                Debug.LogWarning($"Missing prefabPath for id {rec.id}, skipping.");
                continue;
            }

            // ---- Resources variant ----
            var prefab = Resources.Load<GameObject>(rec.prefabPath);

            // If using Addressables:
            // var prefab = await Addressables.LoadAssetAsync<GameObject>(rec.prefabPath).Task;

            if (prefab == null)
            {
                Debug.LogWarning($"Could not load prefab at Resources path '{rec.prefabPath}' for id {rec.id}.");
                continue;
            }

            var go = Instantiate(prefab, rec.position, rec.rotation, spawnRoot);
            var s = go.GetComponent<SaveableObject>();
            if (s == null)
            {
                s = go.AddComponent<SaveableObject>();
#if UNITY_EDITOR
                Debug.LogWarning($"{prefab.name} had no SaveableObject; adding one at runtime so it can persist next save.");
#endif
            }

            s.Apply(rec);     // set id/path + transform
            Register(s);      // ensure registry knows about this instance
        }

        _worldDirty = false;
    }
}
