using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Lightweight highlighter: enables Outline if present; otherwise uses emission color via MPB.
/// Add to the same object as your renderers (or on parent).
/// </summary>
public class SimpleHighlighter : MonoBehaviour
{
    [ColorUsage(true, true)]
    public Color emissionColor = new Color(1f, 0.9f, 0.4f, 1f); // warm glow
    [Range(0f, 5f)] public float intensity = 1.5f;

    private readonly List<Renderer> _renderers = new();
    private readonly Dictionary<Renderer, MaterialPropertyBlock> _blocks = new();
    private Behaviour _outline; // any component named "Outline"

    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        // Cache renderers
        GetComponentsInChildren(true, _renderers);

        // Try find an Outline-like component (common assets use a component literally named "Outline")
        _outline = GetComponent<Behaviour>();
        if (_outline == null || _outline.GetType().Name != "Outline")
        {
            _outline = null; // only use if it's really an Outline behaviour
        }

        // Prepare MPBs
        foreach (var r in _renderers)
        {
            var b = new MaterialPropertyBlock();
            r.GetPropertyBlock(b);
            _blocks[r] = b;
        }
    }

    public void SetHighlighted(bool on)
    {
        if (_outline != null)
        {
            _outline.enabled = on;
            return;
        }

        // MPB emission path
        foreach (var r in _renderers)
        {
            if (!r) continue;
            var b = _blocks[r];
            if (on)
            {
                // enable emission keyword (URP/HDRP/Lit-compatible)
                r.material.EnableKeyword("_EMISSION");
                var col = emissionColor * intensity;
                b.SetColor(EmissionColorID, col);
            }
            else
            {
                b.SetColor(EmissionColorID, Color.black);
            }
            r.SetPropertyBlock(b);
        }
    }
}
