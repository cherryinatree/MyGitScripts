// FlagRegistry.cs
using System.Collections.Generic;
using UnityEngine;

public static class FlagRegistry
{
    static readonly Dictionary<string, BoolFlagSO> _byId = new();
    static readonly HashSet<BoolFlagSO> _all = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        _byId.Clear();
        _all.Clear();
    }

    public static void Register(BoolFlagSO flag)
    {
        if (flag == null) return;
        if (_all.Add(flag))
            _byId[flag.StableId] = flag;
    }

    public static void Unregister(BoolFlagSO flag)
    {
        if (flag == null) return;
        _all.Remove(flag);
        // leave _byId entry; harmless
    }

    public static IEnumerable<BoolFlagSO> AllFlags => _all;
    public static bool TryGet(string id, out BoolFlagSO flag) => _byId.TryGetValue(id, out flag);
}

