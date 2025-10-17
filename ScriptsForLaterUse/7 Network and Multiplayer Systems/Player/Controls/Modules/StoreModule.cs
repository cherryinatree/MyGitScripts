using UnityEngine;
using UnityEngine.InputSystem;

public class StoreModule : ControlModuleBase
{
    public override bool WantsCursorVisible => true;

    public override void Tick(float dt)
    {
        if (Action("Cancel")?.WasPressedThisFrame() == true)
        {
            // Find a store widget and close, or just pop context
            var ctx = GetComponentInParent<InputContextController>();
            ctx?.PopContext();
        }

        // Add UI navigation if you want keyboard/controller navigation too
        // e.g., Action("Navigate"), Action("Submit"), etc.
    }
}
