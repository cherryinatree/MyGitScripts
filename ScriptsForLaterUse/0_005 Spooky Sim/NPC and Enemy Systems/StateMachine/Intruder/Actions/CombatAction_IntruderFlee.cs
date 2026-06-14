using UnityEngine;
using UnityEngine.AI;

[AddComponentMenu("Cherry/AI/Actions/Intruder/Flee")]
public class CombatAction_IntruderFlee : CombatAction
{
    private IntruderMaster _m;
    private float _baseSpeed;
    private bool _saved;

    public override void Initialization()
    {
        base.Initialization();
        _m = GetComponentInParent<IntruderMaster>() ?? GetComponent<IntruderMaster>();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();

        if (_m != null && _m.Agent != null && !_saved)
        {
            _saved = true;
            _baseSpeed = _m.Agent.speed;

            _m.Agent.speed = _baseSpeed * _m.FleeSpeedMultiplier;
            _m.Agent.autoBraking = false;
            _m.Agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }
    }

    public override void PerformAction()
    {
        if (_m == null) { ActionInProgress = false; return; }
        _m.TickFlee();
        ActionInProgress = true;
    }

    public override void OnExitState()
    {
        base.OnExitState();

        if (_m != null && _m.Agent != null && _saved)
        {
            _m.Agent.speed = _baseSpeed;
            _m.Agent.autoBraking = true;
        }

        _saved = false;
    }
}
