using UnityEngine;

[AddComponentMenu("Cherry/AI/Decisions/Intruder/Can Teleport Now")]
public class CombatDecision_IntruderCanTeleportNow : CombatDecision
{
    private IntruderMaster _m;
    private void Awake() => _m = GetComponentInParent<IntruderMaster>() ?? GetComponent<IntruderMaster>();
    public override bool Decide() => _m != null && _m.CanTeleportNow;
}
