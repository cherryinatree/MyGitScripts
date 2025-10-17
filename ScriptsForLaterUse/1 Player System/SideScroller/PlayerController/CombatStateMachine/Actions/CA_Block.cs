using UnityEngine;

public class CA_Block : CombatAction
{
    public GameObject BlockVFX;
    private HealthIntermediate hpIntermediate;

    public override void Initialization()
    {
        base.Initialization();
        BlockVFX.SetActive(false);
        hpIntermediate = GetComponent<HealthIntermediate>();
    }
    public override void PerformAction()
    {
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        hpIntermediate.isBlocking = true;
        BlockVFX.SetActive(true);
    }

    public override void OnExitState()
    {
        base.OnExitState();
        hpIntermediate.isBlocking = false;
        BlockVFX.SetActive(false);
    }

}
