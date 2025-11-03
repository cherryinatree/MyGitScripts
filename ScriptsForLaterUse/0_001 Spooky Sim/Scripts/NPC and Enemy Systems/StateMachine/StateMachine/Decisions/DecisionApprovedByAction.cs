using UnityEngine;
using UnityEngine.Events;


public class DecisionApprovedByAction : CombatDecision
{
    public bool approved = false;
    private bool currentApproval;


    public override void Initialization()
    {
        base.Initialization();
        currentApproval = approved;
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        currentApproval = approved;
    }


    public override bool Decide()
    {
        return currentApproval;
    }

    public void SetDecisionApproved()
    {
        currentApproval = true;
    }
    public void SetDecisionNotApproved()
    {
        currentApproval = false;
    }
}
