using UnityEngine;

public class CA_SpawnVFXenemy : CombatAction
{
    public GameObject VFX_Initial;

    private bool actionComplete = false;

    public override void PerformAction()
    {

        if (stateMachine.targets == null) return;
        if (stateMachine.targets.Count <= 0) return;
        if (VFX_Initial == null) return;

        if (!actionComplete)
        {
            GameObject vfx = Instantiate(VFX_Initial);
            vfx.transform.position = stateMachine.targets[0].transform.position;
            vfx.transform.parent = stateMachine.targets[0].transform;
            actionComplete = true;
        }
    }

    public override void OnEnterState()
    {
        base.OnEnterState();

        actionComplete = false;

    }

    public override void OnExitState()
    {
        base.OnExitState();
        actionComplete = false;
    }
}
