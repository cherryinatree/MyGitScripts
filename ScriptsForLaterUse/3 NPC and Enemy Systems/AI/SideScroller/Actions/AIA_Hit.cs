using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.TextCore.Text;

public class AIA_Hit : AIAction
{
    private Animator animator;
    private HealthIntermediate healthIntermediate;

    public override void PerformAction()
    {
    }

    public override void Initialization()
    {
        base.Initialization();
        animator = GetComponent<Animator>();
        healthIntermediate = GetComponent<HealthIntermediate>();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        Debug.Log("hit");
        GetComponent<CurrentCondition>().currentAnimation = "hit";

        healthIntermediate.LossControl();
        animator.SetTrigger("hit");
    }

    public override void OnExitState()
    {
        base.OnExitState();
        Debug.Log("hit exit");
        healthIntermediate.RegainControl();

    }
}

