using UnityEngine;

[AddComponentMenu("Cherry/AI/Decisions/Robot/Janitor Plan Is")]
public class CombatDecision_JanitorPlanIs : CombatDecision
{
    [SerializeField] private JanitorBrain.Plan plan;
    private JanitorBrain _j;

    private void Awake()
    {
        _j = GetComponentInParent<JanitorBrain>() ?? GetComponent<JanitorBrain>();
    }

    public override bool Decide()
    {
        return _j != null && _j.CurrentPlan == plan;
    }
}
