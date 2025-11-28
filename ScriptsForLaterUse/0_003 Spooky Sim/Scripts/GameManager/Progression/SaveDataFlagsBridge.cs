// SaveDataFlagsBridge.cs
using System.Collections.Generic;
using UnityEngine;

public static class SaveDataFlagsBridge
{
    /// <summary>Copy all runtime flags into SaveData.Current.mainData.progression.flagSOs.</summary>
    public static void PushToSave()
    {
        var list = EnsureList();
        if (list == null) return;

        list.Clear();
        foreach (var flag in FlagRegistry.AllFlags)
        {
            if (flag == null) continue;
            list.Add(new FlagsSaveData { id = flag.StableId, value = flag.Value });
        }
    }

    /// <summary>Apply values from SaveData.Current.mainData.progression.flagSOs back into runtime flags.</summary>
    public static void PullFromSave(bool fireEvents = true)
    {
        var list = GetList();
        if (list == null) return;

        foreach (var entry in list)
        {
            if (string.IsNullOrEmpty(entry.id)) continue;
            if (FlagRegistry.TryGet(entry.id, out var flag) && flag != null)
                flag.Set(entry.value, fireEvents);
        }
    }

    // --- helpers ---
    static List<FlagsSaveData> EnsureList()
    {
        var prog = SaveData.Current?.mainData?.progressionData;
        if (prog == null)
        {
            Debug.LogWarning("SaveDataFlagsBridge: progression is null on SaveData.Current.mainData.");
            return null;
        }
        if (prog.flagSOs == null)
            prog.flagSOs = new List<FlagsSaveData>();
        return prog.flagSOs;
    }

    static List<FlagsSaveData> GetList()
    {
        var prog = SaveData.Current?.mainData?.progressionData;
        if (prog == null)
        {
            Debug.LogWarning("SaveDataFlagsBridge: progression is null on SaveData.Current.mainData.");
            return null;
        }
        return prog.flagSOs;
    }
}
