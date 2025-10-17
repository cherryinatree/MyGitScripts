using UnityEngine;

public class CD_IsAttacking : CombatDecision
{
    public string[] attackTrigger;
    bool _isAttacking = false;
    public override bool Decide()
    {
        return IsAttacking();
    }

    private bool IsAttacking()
    {
        foreach (string attack in attackTrigger)
        {
            if (IsAttacking(attack))
            {

                _isAttacking = true;
                return true;
            }
        }
        if (!_isAttacking)
        {
            return true;
        }
        _isAttacking = false;
        return false;
    }

    private bool IsAttacking(string attack)
    {


        return stateMachine.animator.GetCurrentAnimatorStateInfo(0).IsName(attack);
    }
}