using UnityEngine;

public class CD_HitEnemy : CombatDecision
{
    private bool enemyHit = false;
    public float comboAttackRadius = 1.5f;
    public float checkDelay = 0.25f;
    private bool canCheck = true;

    public override bool Decide()
    {
        if(canCheck)
        {
            canCheck = false;
            Invoke("CheckForEnemy", checkDelay);
        }
       
        if (enemyHit)
        {
            return true;
        }
        return false;
    }

    private void CheckForEnemy()
    {
       /* if (ColliderCheck.SpawnMeleeOnePerson(transform.position, Vector3.right * 1.5f * stateMachine.characterStatus.facingDirection, comboAttackRadius) != null)
        {
            enemyHit = true;
        }*/

        if(stateMachine.targets.Count > 0)
        {
            enemyHit = true;
        }
    }

    public override void Initialization()
    {
        base.Initialization();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        canCheck = true;    
    }
}