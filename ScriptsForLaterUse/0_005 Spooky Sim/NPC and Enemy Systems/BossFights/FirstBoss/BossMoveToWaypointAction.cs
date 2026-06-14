using UnityEngine;

public class BossMoveToWaypointAction : BossCombatAction
{
    public bool ChooseNewWaypointOnEnter = true;
    public bool PreferFarWaypoint = false;
    public bool DashToWaypoint = false;

    public override void OnEnterState()
    {
        base.OnEnterState();
        boss.BeginMoveToWaypoint(ChooseNewWaypointOnEnter, PreferFarWaypoint, DashToWaypoint);
    }

    public override void PerformAction()
    {
        boss.TickMoveToWaypoint();
    }
}