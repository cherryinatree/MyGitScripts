using System.Collections.Generic;
using UnityEngine;

public static class MessRegistry
{
    private static readonly List<MessItem> _all = new();
    public static IReadOnlyList<MessItem> All => _all;

    public static void Register(MessItem item)
    {
        if (item != null && !_all.Contains(item)) _all.Add(item);
    }

    public static void Unregister(MessItem item)
    {
        _all.Remove(item);
    }

    public static MessItem FindNearestUnclaimed(Vector3 from)
    {
        MessItem best = null;
        float bestD = float.PositiveInfinity;

        for (int i = 0; i < _all.Count; i++)
        {
            var m = _all[i];
            if (m == null) continue;
            if (m.IsResolved) continue;
            if (m.IsClaimed) continue;

            float d = Vector3.SqrMagnitude(m.JobPoint - from);
            if (d < bestD)
            {
                bestD = d;
                best = m;
            }
        }
        return best;
    }
}
