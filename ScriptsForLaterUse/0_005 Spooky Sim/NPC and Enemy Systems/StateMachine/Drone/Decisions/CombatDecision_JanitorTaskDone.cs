using UnityEngine;

[AddComponentMenu("Cherry/AI/Decisions/Robot/Janitor Task Done")]
public class CombatDecision_JanitorTaskDone : CombatDecision
{
    private JanitorBrain _j;

    private void Awake()
    {
        _j = GetComponentInParent<JanitorBrain>() ?? GetComponent<JanitorBrain>();
    }

    public override bool Decide()
    {
        return _j != null && _j.TaskDone;
    }
}
