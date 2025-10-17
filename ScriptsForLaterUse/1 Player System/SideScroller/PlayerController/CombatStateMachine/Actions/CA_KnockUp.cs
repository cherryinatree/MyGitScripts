using Unity.VisualScripting;
using UnityEditor.UIElements;
using UnityEngine;

public class CA_KnockUp : CombatAction
{
    public float spawnDelay = 0.25f;
    public float comboAttackRadius = 1.5f;

    private float currentKnockUpTime = 0f;
    public float knockUpDuration = 4f;
    public Vector2 knockUpForce = new Vector2(2, 5);
    public AnimationCurve knockUpGraphX;
    public AnimationCurve knockUpGraphY;

    private bool canAttack = true;

    private GameObject target;
    private Vector3[] origionalPosition;

    [HideInInspector]
    public bool TargetIsKnockedUp = false;

    private float highestGraphYpoint = 0;
    private float highestGraphXpoint = 0;

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
            currentKnockUpTime = 0;

            for (float i = 0; i < 10; i++)
            {
                if (knockUpGraphY.Evaluate(i / 10) > highestGraphYpoint)
                {
                    highestGraphYpoint = knockUpGraphY.Evaluate(i / 10);
                }
                if (knockUpGraphX.Evaluate(i / 10) > highestGraphXpoint)
                {
                    highestGraphXpoint = knockUpGraphX.Evaluate(i / 10);
                }
            }
            
            FindTarget();
        }
        if (stateMachine.targets.Count > 0) KnockUpTarget();
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

    private void KnockUpTarget()
    {
        for (int i = 0; i < stateMachine.targets.Count; i++)
        {
            stateMachine.targets[i].transform.position = CalculateNewPosition(origionalPosition[i]);
        }

    }

    private Vector3 CalculateNewPosition(Vector3 startingPosition)
    {
        Mathf.Clamp(currentKnockUpTime, 0, knockUpDuration);
        currentKnockUpTime += Time.deltaTime;
        Vector3 newPosition = startingPosition + new Vector3(knockUpGraphX.Evaluate(currentKnockUpTime/ knockUpDuration) * knockUpForce.x, 
            knockUpGraphY.Evaluate(currentKnockUpTime/ knockUpDuration) * knockUpForce.y, 0);

        newPosition.x = Mathf.Clamp(newPosition.x, stateMachine.transform.position.x,
            stateMachine.transform.position.x + highestGraphXpoint * knockUpForce.x);
        newPosition.y = Mathf.Clamp(newPosition.y, stateMachine.transform.position.y, 
            stateMachine.transform.position.y + highestGraphYpoint * knockUpForce.y);
        return newPosition;
    }

    public override void Initialization()
    {
        base.Initialization();

    }

    public override void OnEnterState()
    {
        base.OnEnterState();

        canAttack = true;
        target = null;
    }

    public override void OnExitState()
    {
        base.OnExitState();
        canAttack = true;
        target = null;
    }
}