using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Highlights ANY object you look at on the chosen layers by appending an overlay material
/// to its renderers. No per-object components required.
/// </summary>
public class LayerLookHighlighter : MonoBehaviour
{
    [Header("Raycast")]
    public Camera cam;                     // if null, uses Camera.main
    public float maxDistance = 6f;
    public LayerMask highlightLayers;      // set this to your "Highlightable" layer(s)

    [Header("Highlight Style")]
    [Tooltip("Unlit Transparent/Additive material (works in Built-in/URP/HDRP).")]
    public Material overlayMaterial;

    [Header("Scope")]
    [Tooltip("Highlight every renderer under the target's root.")]
    public bool highlightWholeRoot = true;
    [Tooltip("Include inactive children when grabbing renderers.")]
    public bool includeInactiveChildren = true;

    // runtime state
    Transform _currentRoot;
    readonly List<Renderer> _currentRenderers = new();
    readonly Dictionary<Renderer, Material[]> _original = new();

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!overlayMaterial)
            Debug.LogWarning($"{nameof(LayerLookHighlighter)}: No overlayMaterial assigned.");
    }

    void Update()
    {
        var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out var hit, maxDistance, highlightLayers, QueryTriggerInteraction.Ignore))
        {
            Transform root = highlightWholeRoot ? hit.collider.transform.root : hit.collider.transform;
            if (root != _currentRoot)
            {
                ClearCurrent();
                ApplyTo(root);
            }
        }
        else
        {
            ClearCurrent();
        }
    }

    void OnDisable() => ClearCurrent();

    void ApplyTo(Transform root)
    {

        if(!overlayMaterial) return;
        var renderers = includeInactiveChildren ? root.GetComponentsInChildren<Renderer>(true)
                                        : root.GetComponents<Renderer>();


        //_currentRoot = root;

        // Grab renderers
        //Renderer[] renderers = highlightWholeRoot
        //    ? root.GetComponentsInChildren<Renderer>(includeInactiveChildren)
         //   : root.GetComponents<Renderer>();

        foreach (var r in renderers)
        {
            if (!r) continue;

            // Cache original array for later restore
            if (!_original.ContainsKey(r))
                _original.Add(r, r.sharedMaterials);

            // Append overlay if not already appended
            var arr = r.sharedMaterials;
            if (arr.Length > 0 && arr[arr.Length - 1] == overlayMaterial)
            {
                _currentRenderers.Add(r);
                continue;
            }

            var newArr = new Material[arr.Length + 1];
            for (int i = 0; i < arr.Length; i++) newArr[i] = arr[i];
            newArr[newArr.Length - 1] = overlayMaterial;
            r.sharedMaterials = newArr;

            _currentRenderers.Add(r);
        }
    }

    void ClearCurrent()
    {
        if (_currentRenderers.Count == 0) { _currentRoot = null; return; }

        // Restore original sharedMaterials for everything we touched
        foreach (var r in _currentRenderers)
        {
            if (!r) continue;
            if (_original.TryGetValue(r, out var orig) && orig != null)
                r.sharedMaterials = orig;
        }

        _currentRenderers.Clear();
        _original.Clear();
        _currentRoot = null;
    }
}
