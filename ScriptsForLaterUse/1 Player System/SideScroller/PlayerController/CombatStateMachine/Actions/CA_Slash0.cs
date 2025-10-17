using UnityEngine;

public class CA_Slash0 : CombatAction
{
    public string attackTrigger = "attack1";
    private bool attackStarted = false;

    public override void Initialization()
    {
        base.Initialization();
    }

    public override void PerformAction()
    {
        if (!attackStarted)
        {
            if (stateMachine.animator.GetCurrentAnimatorStateInfo(0).IsName(attackTrigger))
            {
                attackStarted = true;
            }
        }
        else
        {

            if (!stateMachine.animator.GetCurrentAnimatorStateInfo(0).IsName(attackTrigger))
            {
                stateMachine.isAttackFinished = true;
            }
        }

    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        stateMachine.isAttackFinished = false; 
        attackStarted = false;
        stateMachine.animator.SetTrigger(attackTrigger);

    }

    public override void OnExitState()
    {
        base.OnExitState();
        stateMachine.isAttackFinished = false;
        attackStarted = false;
    }
}