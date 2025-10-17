using UnityEngine;
using UnityEngine.InputSystem;

public class LocomotionModule : ControlModuleBase
{
    [SerializeField] MonoBehaviour motorRef; // IMotor
    IMotor _motor;

    public override void Activate()
    {
        base.Activate();
        _motor = motorRef as IMotor;
        if (_motor == null) _motor = GetComponentInParent<IMotor>();
        if (_motor == null) Debug.LogError("[LocomotionModule] IMotor not found.");

        // Ensure look deltas are not accumulated when inactive
        _lookAccum = Vector2.zero;
    }

    public override void Tick(float dt)
    {
        if (PlayerInput == null || _motor == null) return;

        var move = Action("Move")?.ReadValue<Vector2>() ?? Vector2.zero;
        var look = Action("Look")?.ReadValue<Vector2>() ?? Vector2.zero;
        var jump = Action("Jump")?.WasPressedThisFrame() ?? false;
        var sprint = Action("Sprint")?.IsPressed() ?? false;

        // Pass to motor
        _motor.SetMoveInput(move);
        _motor.SetLookInput(look);
        _motor.SetSprinting(sprint);
        if (jump) _motor.Jump();
    }

    Vector2 _lookAccum;
}
