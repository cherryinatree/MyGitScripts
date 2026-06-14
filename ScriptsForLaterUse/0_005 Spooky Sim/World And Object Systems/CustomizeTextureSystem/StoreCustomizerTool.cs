using UnityEngine;
using UnityEngine.InputSystem;

public class StoreCustomizerTool : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera viewCamera;
    [SerializeField] private float range = 4f;
    [SerializeField] private LayerMask surfaceMask = ~0;

    [Header("Allowed Surfaces")]
    public bool allowFloor = true;
    public bool allowWall = true;
    public bool allowCeiling = true;

    [Header("Input (New Input System)")]
    [SerializeField] private InputActionReference applyAction;    // e.g. Left Click or Interact
    [SerializeField] private InputActionReference openUiAction;   // bind to <Mouse>/rightButton

    [Header("UI")]
    [SerializeField] private StoreCustomizerSelectionUI selectionUI;
    // Add fields:
    [SerializeField] private bool hoverHighlight = true;

    private StoreSurface _hoveredSurface;

    [Header("Current Selection (set by UI)")]
    public FinishSelection currentSelection = default;

    private void OnEnable()
    {
        if (!viewCamera) viewCamera = Camera.main;
        if (applyAction) applyAction.action.Enable();
        if (openUiAction) openUiAction.action.Enable();
    }

    private void OnDisable()
    {
        if (applyAction) applyAction.action.Disable();
        if (openUiAction) openUiAction.action.Disable();
    }

    private void Update()
    {
        if (openUiAction != null && openUiAction.action.WasPressedThisFrame())
        {
            if (selectionUI) selectionUI.Toggle();
        }

        // While UI open: clear highlight + block apply
        if (selectionUI != null && selectionUI.IsOpen)
        {
            ClearHover();
            return;
        }

        if (hoverHighlight) UpdateHover();

        if (applyAction != null && applyAction.action.WasPressedThisFrame())
            TryApply();
    }

    private void UpdateHover()
    {
        if (!viewCamera) return;

        Ray ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, range, surfaceMask, QueryTriggerInteraction.Ignore))
        {
            ClearHover();
            return;
        }

        var surface = hit.collider.GetComponentInParent<StoreSurface>();
        if (surface == null || !IsAllowed(surface.surfaceType))
        {
            ClearHover();
            return;
        }

        if (_hoveredSurface == surface) return;

        ClearHover();
        _hoveredSurface = surface;
        //_hoveredSurface.SetHovered(true);
    }

    private void ClearHover()
    {
        if (_hoveredSurface != null)
        {
            //_hoveredSurface.SetHovered(false); 
            _hoveredSurface = null;
        }
    }

    public void SetSelection(FinishSelection selection) => currentSelection = selection;

    private void TryApply()
    {
        if (!viewCamera || currentSelection.material == null) return;

        var ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, range, surfaceMask, QueryTriggerInteraction.Ignore))
            return;

        var surface = hit.collider.GetComponentInParent<StoreSurface>();
        if (!surface) return;

        if (!IsAllowed(surface.surfaceType)) return;
        surface.ApplyFinish(currentSelection.finishId,
                    currentSelection.material,
                    currentSelection.tint,
                    currentSelection.tiling);

    }

    private bool IsAllowed(SurfaceType type) => type switch
    {
        SurfaceType.Floor => allowFloor,
        SurfaceType.Wall => allowWall,
        SurfaceType.Ceiling => allowCeiling,
        _ => false
    };
}
