using MoreMountains.Tools;
using UnityEngine;

public class CD_AttackButtonPressed : CombatDecision
{
    public override bool Decide()
    {
        return stateMachine.inputManager.RunButton.State.CurrentState == MMInput.ButtonStates.ButtonUp;
    }
}
