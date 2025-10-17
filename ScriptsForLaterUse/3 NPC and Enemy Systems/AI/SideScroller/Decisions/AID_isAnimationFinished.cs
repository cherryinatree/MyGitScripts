using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

public class AID_isAnimationFinished : AIDecision
{
    bool animationStarted = false;
    private Animator animator;
    private CurrentCondition currentCondition;
    public override bool Decide()
    {
        if (animationStarted)
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName(currentCondition.currentAnimation))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName(currentCondition.currentAnimation))
            {
                animationStarted = true;
            }
             
            return false;
        }

    }

    public override void Initialization()
    {
        base.Initialization();
        animationStarted = false;
        animator = GetComponent<Animator>();
        currentCondition = GetComponent<CurrentCondition>();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        animationStarted = false;
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if(currentCondition == null)
        {
            currentCondition = GetComponent<CurrentCondition>();
        }
    }

    public override void OnExitState()
    {
        base.OnExitState();
        animationStarted = false;
    }


}

