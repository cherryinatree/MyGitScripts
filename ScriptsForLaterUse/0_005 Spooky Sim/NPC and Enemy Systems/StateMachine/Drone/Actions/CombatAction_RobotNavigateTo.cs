using UnityEngine;

public class CombatAction_RobotNavigateTo : CombatAction
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 worldPosition;
    [SerializeField] private bool useTransformTarget = true;

    private RobotNavigator _nav;

    public override void Initialization()
    {
        base.Initialization();
        _nav = GetComponentInParent<RobotNavigator>();
        if (_nav == null) _nav = GetComponent<RobotNavigator>();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        if (_nav == null) Initialization();

        Vector3 goal = (useTransformTarget && target != null) ? target.position : worldPosition;
        _nav.SetGoal(goal);
    }

    public override void PerformAction()
    {
        if (_nav == null)
        {
            ActionInProgress = false;
            return;
        }

        // ✅ NOTE THE PARENTHESES HERE:
        ActionInProgress = !_nav.ReachedGoal();
    }
}
