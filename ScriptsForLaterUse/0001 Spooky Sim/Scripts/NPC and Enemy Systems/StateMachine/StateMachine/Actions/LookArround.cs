using UnityEngine;
using UnityEngine.Events;

public class LookArround : CombatAction
{

    public UnityEvent onArrivedAtItem;

    public override void OnEnterState()
    {
        base.OnEnterState();
    }

    public override void PerformAction()
    {
        // Do nothing, just wait in line
    }
}
