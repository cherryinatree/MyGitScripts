using UnityEngine;

[AddComponentMenu("Cherry/AI/Decisions/Intruder/Has Threat")]
public class CombatDecision_IntruderHasThreat : CombatDecision
{
    private IntruderMaster _m;
    private void Awake() => _m = GetComponentInParent<IntruderMaster>() ?? GetComponent<IntruderMaster>();
    public override bool Decide() => _m != null && _m.HasThreat;
}
