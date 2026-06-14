using UnityEngine;

public class DecisionApprovedByAction : CombatDecision
{
    [Header("Default (used when entering the state)")]
    [SerializeField] private bool defaultApproved = false;

    // This is the ONLY value Decide() uses
    private bool _approved;

    public override void Initialization()
    {
        base.Initialization();
        _approved = defaultApproved;
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        _approved = defaultApproved;
    }
    public override bool Decide()
    {

        //Debug.Log("DecisionApprovedByAction: Decide() called, returning " + _approved + " Label: " + Label);
        return _approved;
    }

    // Call these from actions / UnityEvents
    public void SetDecisionApproved()
    {
        //Debug.Log("DecisionApprovedByAction: Decision set to APPROVED" + _approved + " Label: " + Label);
        _approved = true;
    }

    public void SetDecisionNotApproved()
    {
        _approved = false;
    }

    // Optional: helpful when debugging in inspector
    public bool Debug_IsApproved => _approved;
}
