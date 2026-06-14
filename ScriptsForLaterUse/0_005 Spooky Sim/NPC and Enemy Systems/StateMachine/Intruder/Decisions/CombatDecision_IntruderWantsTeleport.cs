using UnityEngine;

[AddComponentMenu("Cherry/AI/Decisions/Intruder/Wants Teleport")]
public class CombatDecision_IntruderWantsTeleport : CombatDecision
{
    private IntruderMaster _m;
    private void Awake() => _m = GetComponentInParent<IntruderMaster>() ?? GetComponent<IntruderMaster>();
    public override bool Decide() => _m != null && _m.WantsTeleport;
}
