using UnityEngine;

[AddComponentMenu("Cherry/AI/Actions/Robot/Janitor Think")]
public class CombatAction_JanitorThink : CombatAction
{
    private JanitorBrain _j;

    public override void Initialization()
    {
        base.Initialization();
        _j = GetComponentInParent<JanitorBrain>() ?? GetComponent<JanitorBrain>();
    }

    public override void PerformAction()
    {
        if (_j == null) { ActionInProgress = false; return; }
        _j.DecidePlan();
        ActionInProgress = false; // Think is instantaneous
    }
}
