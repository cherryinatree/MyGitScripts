using UnityEngine;
using UnityEngine.Events;

public class Despawn : CombatAction
{

    public UnityEvent onArrivedAtItem;

    public override void OnEnterState()
    {
        base.OnEnterState();
        Destroy(gameObject);
    }

    public override void PerformAction()
    {
        // Do nothing, just wait in line
    }
}
