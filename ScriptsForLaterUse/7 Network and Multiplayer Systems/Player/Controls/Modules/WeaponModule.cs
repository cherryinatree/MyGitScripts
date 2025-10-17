using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponModule : ControlModuleBase
{
    public override void Tick(float dt)
    {
        var fire = Action("Fire")?.IsPressed() ?? false;
        var aim = Action("Aim")?.IsPressed() ?? false;
        var reload = Action("Reload")?.WasPressedThisFrame() ?? false;

        // TODO: call into your weapon system interfaces (client-predicted, server-authoritative RPCs)
        if (reload) Debug.Log("Reload");
        if (fire) Debug.Log("Firing");
        if (aim) Debug.Log("Aiming");
    }
}
