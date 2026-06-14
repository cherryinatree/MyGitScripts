// ItemDatabase.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemDefinition> items = new();
    Dictionary<string, ItemDefinition> map;

    public void Init()
    {
        if (map != null) return;
        map = new();
        foreach (var i in items)
        {
            if (i && !string.IsNullOrEmpty(i.itemId)) map[i.itemId] = i;
            else Debug.LogWarning($"ItemDatabase: Missing itemId on {i?.name}");
        }
    }

    public ItemDefinition Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        Init();
        return map.TryGetValue(id, out var def) ? def : null;
    }
}
