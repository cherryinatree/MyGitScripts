using UnityEngine;

public class CD_Jumping : CombatDecision
{
    public override bool Decide()
    {
        return IsJumping();
    }

    private bool IsJumping()
    {
        return !stateMachine.controller.State.IsGrounded;
    }

}
