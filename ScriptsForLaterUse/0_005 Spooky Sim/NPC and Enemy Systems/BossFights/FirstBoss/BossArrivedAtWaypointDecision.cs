using UnityEngine;

public class BossArrivedAtWaypointDecision : BossCombatDecision
{
    public override bool Decide()
    {
        return boss.ArrivedAtWaypoint;
    }
}