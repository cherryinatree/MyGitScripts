using MoreMountains.CorgiEngine;
using UnityEngine;

public class CA_Hit : CombatAction
{
    //private Animator animator;
    private float health;
    private Health myHealth;
    private CorgiController controller;
    private Character myCharacter;
    private HealthIntermediate healthIntermediate;
    [Range(0, 1)]
    public float hitFinishedTime = 0.8f;

    public override void PerformAction()
    {
        /*if (myHealth.CurrentHealth < health)
        {
            health = myHealth.CurrentHealth;
            animator.StopPlayback();
            animator.Play("hit");
        }*/

        if(stateMachine.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= hitFinishedTime)
        {
            stateMachine.isAttackFinished = true;
        }
    }

    public override void Initialization()
    {
        base.Initialization();
        //animator = GetComponent<Animator>();
        myHealth = GetComponent<Health>();
        controller = GetComponent<CorgiController>();
        myCharacter = GetComponent<Character>();
        healthIntermediate = GetComponent<HealthIntermediate>();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        Debug.Log("hit");
        GetComponent<CurrentCondition>().currentAnimation = "hit";
        stateMachine.isAttackFinished = false;
        //myCharacter.ChangeCharacterConditionTemporarily(CharacterStates.CharacterConditions.Stunned, 0.5f, false, false);

        stateMachine.animator.Play("hit");

        health = myHealth.CurrentHealth;
    }

    public override void OnExitState()
    {
        base.OnExitState();
        stateMachine.isAttackFinished = false;
        healthIntermediate.RegainControl();
        //myCharacter.ChangeCharacterConditionTemporarily(CharacterStates.CharacterConditions.Normal, 0.5f, false, false);

    }
}

