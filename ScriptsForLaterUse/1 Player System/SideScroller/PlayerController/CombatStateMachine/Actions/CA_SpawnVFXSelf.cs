using UnityEngine;

public class CA_SpawnVFXSelf : CombatAction
{
    public GameObject VFX_Initial;

    private bool actionComplete = false;

    public override void PerformAction()
    {


        if(VFX_Initial == null) return;

        if (!actionComplete)
        {
            GameObject vfx = Instantiate(VFX_Initial);

            vfx.transform.position = stateMachine.transform.position;
            vfx.transform.parent = stateMachine.transform;
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