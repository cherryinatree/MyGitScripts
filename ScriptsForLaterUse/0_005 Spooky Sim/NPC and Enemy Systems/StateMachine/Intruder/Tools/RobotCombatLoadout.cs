using UnityEngine;

[DisallowMultipleComponent]
public class RobotCombatLoadout : MonoBehaviour
{
    public enum AttackMode { Melee, Ranged }

    [Header("Availability")]
    public bool HasMelee = true;
    public bool HasRanged = false;

    [Header("Melee")]
    public float MeleeRange = 1.8f;
    public float MeleeDamage = 25f;
    public float MeleeCooldown = 1.0f;
    public string MeleeAttackTrigger = "AttackMelee";
    public float MeleeHitRadius = 0.6f;
    public Vector3 MeleeHitOffset = new Vector3(0f, 1.0f, 1.0f); // local offset from robot

    [Header("Ranged")]
    public float RangedRange = 10f;
    public float RangedDamage = 15f;
    public float RangedCooldown = 1.2f;
    public string RangedAttackTrigger = "AttackRanged";
    public Transform Muzzle;
    public GameObject ProjectilePrefab;
    public float ProjectileSpeed = 22f;

    public AttackMode ChooseMode(Intruder target)
    {
        if (target != null)
        {
            var profile = target.GetComponent<IntruderCombatProfile>();
            if (profile != null && profile.ForceMeleeOnly) return AttackMode.Melee;
            if (HasRanged && profile != null && profile.PreferRangedAgainstThisIntruder) return AttackMode.Ranged;
        }

        if (HasRanged) return AttackMode.Ranged;
        return AttackMode.Melee;
    }

    public float GetRange(AttackMode mode) => mode == AttackMode.Ranged ? RangedRange : MeleeRange;
    public float GetCooldown(AttackMode mode) => mode == AttackMode.Ranged ? RangedCooldown : MeleeCooldown;
    public float GetDamage(AttackMode mode) => mode == AttackMode.Ranged ? RangedDamage : MeleeDamage;
    public string GetTrigger(AttackMode mode) => mode == AttackMode.Ranged ? RangedAttackTrigger : MeleeAttackTrigger;
}
