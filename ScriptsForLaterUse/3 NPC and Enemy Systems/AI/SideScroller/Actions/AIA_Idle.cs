using MoreMountains.Tools;
using UnityEngine;

public class AIA_Idle : AIAction
{
    private Animator animator;
    public override void PerformAction()
    {
        //throw new System.NotImplementedException();
    }

    public override void Initialization()
    {
        base.Initialization();
        animator = GetComponent<Animator>();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        Debug.Log("idle");
        
    }

    public override void OnExitState()
    {
        base.OnExitState();

    }
}


