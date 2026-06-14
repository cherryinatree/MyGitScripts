using UnityEngine;

[AddComponentMenu("Cherry/AI/Decisions/Intruder/Wants Teleport AND Can Teleport")]
public class CombatDecision_IntruderWantsTeleportAndCan : CombatDecision
{
    private IntruderMaster _m;

    private void Awake()
    {
        _m = GetComponentInParent<IntruderMaster>() ?? GetComponent<IntruderMaster>();
    }

    public override bool Decide()
    {
        if (_m == null) return false;
        return _m.WantsTeleport && _m.CanTeleportNow;
    }
}
