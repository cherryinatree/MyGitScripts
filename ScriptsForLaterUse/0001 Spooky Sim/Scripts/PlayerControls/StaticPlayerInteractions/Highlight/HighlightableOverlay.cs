using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HighlightableOverlay : MonoBehaviour, IHighlightable
{
    [Header("Overlay Material (Unlit, Transparent/Additive)")]
    public Material highlightMaterial;

    [Header("Scope")]
    public bool includeChildren = true;

    readonly Dictionary<Renderer, Material[]> _original = new();
    bool _isOn;

    void Awake()
    {
        CacheOriginals();
    }

    void OnEnable()
    {
        if (_isOn) ApplyOverlay(true);
    }

    void OnDisable()
    {
        if (_isOn) ApplyOverlay(false);
    }

    public void SetHighlighted(bool on)
    {
        if (_isOn == on) return;
        _isOn = on;
        ApplyOverlay(on);
    }

    void CacheOriginals()
    {
        _original.Clear();
        var renderers = includeChildren ? GetComponentsInChildren<Renderer>(true)
                                        : GetComponents<Renderer>();

        foreach (var r in renderers)
        {
            if (!_original.ContainsKey(r))
                _original.Add(r, r.sharedMaterials); // store shared array (original)
        }
    }

    void ApplyOverlay(bool enable)
    {
        if (!highlightMaterial)
        {
            Debug.LogWarning($"{name}: No highlightMaterial set.");
            return;
        }

        foreach (var kv in _original)
        {
            var r = kv.Key;
            if (!r) continue;

            if (enable)
            {
                // Append highlight material if not already appended
                var arr = r.sharedMaterials;
                if (arr.Length > 0 && arr[arr.Length - 1] == highlightMaterial) continue;

                var newArr = new Material[arr.Length + 1];
                for (int i = 0; i < arr.Length; i++) newArr[i] = arr[i];
                newArr[newArr.Length - 1] = highlightMaterial;
                r.sharedMaterials = newArr;
            }
            else
            {
                // Restore to the original array
                r.sharedMaterials = kv.Value;
            }
        }
    }
}
