using System.Collections.Generic;

public static class StoreSurfaceRegistry
{
    private static readonly HashSet<StoreSurface> _all = new();

    public static IEnumerable<StoreSurface> All => _all;

    public static void Register(StoreSurface s)
    {
        if (s != null) _all.Add(s);
    }

    public static void Unregister(StoreSurface s)
    {
        if (s != null) _all.Remove(s);
    }
}
