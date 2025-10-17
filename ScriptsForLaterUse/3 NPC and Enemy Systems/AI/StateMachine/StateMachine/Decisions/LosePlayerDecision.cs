using UnityEngine;

public class LosePlayerDecision : CombatDecision
{
    private MonsterPerception perception;

    protected override void Awake()
    {
        base.Awake();
        perception = GetComponentInParent<MonsterPerception>();
    }

    public override bool Decide()
    {
        return perception != null && !perception.IsPlayerSpotted;
    }
}
