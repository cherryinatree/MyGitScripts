using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Attach to any icon/button/setting on your in-game computer.
/// It "activates" when the mouse hotspot overlaps it (hover),
/// and runs the appropriate click event when PControls fires Single/Double.
/// Works with:
///  - Collider2D/3D triggers (Physics hit test), OR
///  - RectTransform containment (for pure UI, no colliders)
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PCClickable : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent OnHoverEnter;
    public UnityEvent OnHoverExit;
    public UnityEvent OnSingleClick;
    public UnityEvent OnDoubleClick;

    [Header("Optional Visuals")]
    [Tooltip("If assigned, this Image will be tinted on hover.")]
    public Image highlightImage;
    public Color hoverColor = new Color(1f, 1f, 1f, 0.15f);
    private Color _baseColor;
    private bool _hover;

    private RectTransform _rt;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (highlightImage != null)
            _baseColor = highlightImage.color;
    }

    /// <summary>Called by PControls when hover enters/exits.</summary>
    public void SetHover(bool hover)
    {
        if (_hover == hover) return;
        _hover = hover;

        if (_hover)
        {
            if (highlightImage != null) highlightImage.color = hoverColor;
            OnHoverEnter?.Invoke();
        }
        else
        {
            if (highlightImage != null) highlightImage.color = _baseColor;
            OnHoverExit?.Invoke();
        }
    }

    /// <summary>Called by PControls with the resolved click kind (single / double).</summary>
    public void HandleClick(ClickKind kind)
    {
        Debug.Log($"PCClickable.HandleClick: kind={kind}, hover={_hover}");
        if (!_hover) return; // only respond when under hotspot
        if (kind == ClickKind.Double) OnDoubleClick?.Invoke();
        else OnSingleClick?.Invoke();
    }

    /// <summary>
    /// For "UIRectContains" hit test mode: return true if the given screenPoint lies within this element.
    /// </summary>
    public bool TryRectContains(Vector2 screenPoint, Camera uiCamera)
    {
        if (_rt == null || !isActiveAndEnabled) return false;
        // If on a world-space canvas, pass its world camera; if overlay, camera may be null
        return RectTransformUtility.RectangleContainsScreenPoint(_rt, screenPoint, uiCamera);
    }
}
