using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

public class AID_isJumping : AIDecision
{
    private CorgiController _controller;

    public override void Initialization()
    {
        base.Initialization();
        _controller = GetComponent<CorgiController>();
    }

    public override bool Decide()
    {
        return IsJumping();
    }

    private bool IsJumping()
    {
        return !_controller.State.IsGrounded;
    }
}
