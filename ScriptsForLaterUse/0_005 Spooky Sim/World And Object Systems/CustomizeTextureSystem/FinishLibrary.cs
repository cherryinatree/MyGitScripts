using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Store Customizer/Material Finish Library", fileName = "FinishMaterialLibrary")]
public class FinishMaterialLibrary : ScriptableObject
{
    public List<FinishMaterialEntry> finishes = new();

    private Dictionary<string, FinishMaterialEntry> _byId;

    public bool TryGetById(string id, out FinishMaterialEntry entry)
    {
        if (_byId == null) BuildLookup();
        return _byId.TryGetValue(id, out entry);
    }

    public void BuildLookup()
    {
        _byId = new Dictionary<string, FinishMaterialEntry>(StringComparer.Ordinal);
        foreach (var f in finishes)
        {
            if (f == null || string.IsNullOrWhiteSpace(f.id)) continue;
            if (!_byId.ContainsKey(f.id))
                _byId.Add(f.id, f);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        bool changed = false;
        foreach (var f in finishes)
        {
            if (f == null) continue;
            if (string.IsNullOrWhiteSpace(f.id))
            {
                f.id = Guid.NewGuid().ToString("N");
                changed = true;
            }
        }

        if (changed)
        {
            UnityEditor.EditorUtility.SetDirty(this);
            BuildLookup();
        }
#endif
    }
}


[Serializable]
public class FinishMaterialEntry
{
    [Tooltip("Stable id used for saving/loading. Do not change once in use.")]
    public string id;

    public string displayName;
    public Material material;

    public Sprite thumbnail;
    public Texture2D previewTexture;

    public Color defaultTint = Color.white;
    public Vector2 defaultTiling = Vector2.one;
}

