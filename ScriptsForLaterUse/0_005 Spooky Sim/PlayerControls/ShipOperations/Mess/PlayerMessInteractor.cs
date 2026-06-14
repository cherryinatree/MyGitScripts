using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMessInteractor : MonoBehaviour
{
    public Camera Cam;
    public float UseRange = 3f;
    public InputActionReference UseAction;

    private void OnEnable()
    {
        if (UseAction != null) UseAction.action.Enable();
    }

    private void OnDisable()
    {
        if (UseAction != null) UseAction.action.Disable();
    }

    private void Update()
    {
        if (UseAction == null || !UseAction.action.WasPressedThisFrame()) return;
        if (Cam == null) Cam = Camera.main;
        if (Cam == null) return;

        Ray r = new Ray(Cam.transform.position, Cam.transform.forward);
        if (!Physics.Raycast(r, out var hit, UseRange, ~0, QueryTriggerInteraction.Ignore))
            return;

        var mess = hit.collider.GetComponentInParent<MessItem>();
        if (mess != null)
        {
            mess.Interact(gameObject);
            return;
        }

        var bin = hit.collider.GetComponentInParent<TrashDisposalBin>();
        if (bin != null)
        {
            bin.Interact(gameObject);
            return;
        }
    }
}
