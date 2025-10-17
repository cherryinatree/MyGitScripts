using MoreMountains.CorgiEngine;
using UnityEngine;

public class CA_CatchUp : CombatAction
{
    public float distanceToTarget = 1.25f;
    public float speed = 100f;


    private bool canAttack = true;

    private GameObject target;
    private Vector3[] origionalPosition;

    [HideInInspector]
    public bool TargetIsKnockedBack = false;

    Vector3 newPosition;

    public override void PerformAction()
    {
        if (ShouldInitialize)
        {
            Initialization();
        }

        if (stateMachine.targets == null) return;
        if (stateMachine.targets.Count <= 0) return;
        if (canAttack)
        {
            canAttack = false;

            FindTarget();

        }
        if (stateMachine.targets.Count > 0) FollowTarget();
    }

    public override void OnExitState()
    {
        base.OnExitState();
        canAttack = true;
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        canAttack = true;
    
    }

    private void FindTarget()
    {
        //target = ColliderCheck.SpawnMeleeOnePerson(transform.position, Vector3.right*1.5f*stateMachine.characterStatus.facingDirection, comboAttackRadius);


        if (stateMachine.targets.Count > 0)
        {
            origionalPosition = new Vector3[stateMachine.targets.Count];

            for (int i = 0; i < stateMachine.targets.Count; i++)
            {
                origionalPosition[i] = stateMachine.targets[i].transform.position;
            }
        }

    }

    private void FollowTarget()
    {
      /*  if (Vector3.Distance(stateMachine.transform.position, new Vector3(origionalPosition[0].x - distanceToTarget, 
            stateMachine.transform.position.y,stateMachine.transform.position.z)) > 0.01f)
        {

        }*/
        stateMachine.transform.position = Vector3.MoveTowards(stateMachine.transform.position,
            new Vector3(origionalPosition[0].x - distanceToTarget,
        stateMachine.transform.position.y, stateMachine.transform.position.z), speed * Time.deltaTime);
        //stateMachine.targets[0].transform.position = origionalPosition[0];

    }
}
