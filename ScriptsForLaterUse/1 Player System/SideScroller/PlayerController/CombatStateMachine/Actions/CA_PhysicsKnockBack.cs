using MoreMountains.CorgiEngine;
using UnityEngine;

public class CA_PhysicsKnockBack : CombatAction
    {
    public Vector2 knockBackForce = new Vector2(5, 2);

    private bool canAttack = true;

    private Vector3[] origionalPosition;

    [HideInInspector]
    public bool TargetIsKnockedBack = false;

    public override void PerformAction()
    {
        if (ShouldInitialize)
        {
            Initialization();
        }

        if(stateMachine.targets == null) return;
        if(stateMachine.targets.Count <= 0) return;
        if (canAttack)
        {
            canAttack = false; 
            
            FindTarget();
            KnockBackTarget();
        }
        //if (stateMachine.targets.Count > 0) KnockBackTarget();
    }
    public override void OnEnterState()
    {
        base.OnEnterState();
        canAttack = true;
    
    }
    public override void OnExitState()
    {
        base.OnExitState();
        canAttack = true;
    }

    private void FindTarget()
    {
        //target = ColliderCheck.SpawnMeleeOnePerson(transform.position, Vector3.right*1.5f*stateMachine.characterStatus.facingDirection, comboAttackRadius);
        

        if(stateMachine.targets.Count > 0)
        {
            origionalPosition = new Vector3[stateMachine.targets.Count];
        

            for(int i = 0; i < stateMachine.targets.Count; i++)
            {
                origionalPosition[i] = stateMachine.targets[i].transform.position;
            }
        }

    }

    private void KnockBackTarget()
    {
        for(int i = 0; i < stateMachine.targets.Count; i++)
        {
            stateMachine.targets[i].GetComponent<CorgiController>().AddForce(knockBackForce);  
        }
    }
}
