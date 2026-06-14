using UnityEngine;

[AddComponentMenu("Cherry/AI/Decisions/World/Intruder Alert Active")]
public class CombatDecision_IntruderAlertActive : CombatDecision
{
    public override bool Decide()
    {
        return IntruderAlertSystem.AlertActive;
    }
}
