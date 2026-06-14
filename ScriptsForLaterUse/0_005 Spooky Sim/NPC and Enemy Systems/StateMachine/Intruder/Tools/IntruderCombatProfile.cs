using UnityEngine;

[DisallowMultipleComponent]
public class IntruderCombatProfile : MonoBehaviour
{
    public bool ForceMeleeOnly = false;
    public bool PreferRangedAgainstThisIntruder = true;

    [Tooltip("If robot is ranged, it will try to keep about this distance.")]
    public float PreferredRangedDistance = 8f;
}
