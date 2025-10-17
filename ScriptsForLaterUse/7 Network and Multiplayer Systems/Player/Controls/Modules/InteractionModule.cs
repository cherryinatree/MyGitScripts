using UnityEngine;

/// Simple proximity check for demo; replace with your interactable detector
public class InteractionModule : ControlModuleBase
{
    [SerializeField] float interactRange = 2.0f;
    InputContextController _ctx;

    protected override void Awake()
    {
        base.Awake();
        _ctx = GetComponentInParent<InputContextController>();
    }

    public override void Tick(float dt)
    {
        if (Action("Interact")?.WasPressedThisFrame() == true)
        {
            // Raycast or overlap for an Interactable
            if (TryFindStore(out var store))
            {
                store.OpenForLocalPlayer(_ctx);
            }
            else
            {
                // other interactions here…
            }
        }
    }

    bool TryFindStore(out StoreInteractable store)
    {
        store = null;
        var tr = Camera.main ? Camera.main.transform : transform;
        if (Physics.Raycast(tr.position, tr.forward, out var hit, interactRange))
            store = hit.collider.GetComponentInParent<StoreInteractable>();
        return store != null;
    }
}
