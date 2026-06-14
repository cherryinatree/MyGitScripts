using UnityEngine;

[DisallowMultipleComponent]
public class RobotCombatController : MonoBehaviour
{
    public RobotCombatLoadout Loadout;
    public RobotNavigator Navigator;
    public Animator Anim;

    [Header("Projectile Spawn")]
    public Transform MuzzleOverride;

    [Header("Aiming (Ranged)")]
    [Tooltip("A bone or transform to rotate so the gun aims at the target (eg. gun root / hand bone).")]
    public Transform AimPivot;
    public float AimTurnSpeed = 1440f; // degrees/sec
    public Vector3 AimWorldOffset = new Vector3(0f, 1.2f, 0f); // aim at chest/head

    [Header("Fallback timing (also used as safety net)")]
    public bool UseAnimationEvents = true;
    public float FallbackHitDelay = 0.15f;   // usually earlier for guns
    public float FallbackAttackEnd = 0.6f;

    [Header("Debug")]
    public bool DebugCombat = false;

    public Intruder CurrentTarget { get; private set; }
    public RobotCombatLoadout.AttackMode CurrentMode { get; private set; }
    public float CurrentRange { get; private set; }
    public bool IsAttacking { get; private set; }

    private float _nextAttackAllowed;
    private float _attackStartTime;
    private bool _hitDone;

    private void Awake()
    {
        if (Loadout == null) Loadout = GetComponentInParent<RobotCombatLoadout>() ?? GetComponent<RobotCombatLoadout>();
        if (Navigator == null) Navigator = GetComponentInParent<RobotNavigator>() ?? GetComponent<RobotNavigator>();
        if (Anim == null) Anim = GetComponentInChildren<Animator>();
    }

    private void Log(string msg)
    {
        //if (DebugCombat) Debug.Log($"[RobotCombat:{name}] {msg}");
    }

    private void LateUpdate()
    {
        // LateUpdate runs after Animator, so this "wins" against animation posing
        if (!IsAttacking) return;
        if (CurrentMode != RobotCombatLoadout.AttackMode.Ranged) return;
        if (CurrentTarget == null) return;

        AimGunToward(AimPointWorld());
    }

    private void Update()
    {
        if (!IsAttacking) return;

        // Safety net: if animation events aren't wired, still apply hit + end
        float t = Time.time - _attackStartTime;

        if (!_hitDone && t >= FallbackHitDelay)
        {
            _hitDone = true;
            ApplyHit();
        }

        if (t >= FallbackAttackEnd)
            EndAttack();
    }

    public bool CanAttackNow() => Time.time >= _nextAttackAllowed;

    public bool TryStartAttack(Intruder target)
    {
        if (target == null) return false;

        var th = target.GetComponent<Health>();
        if (th != null && th.IsDead) return false;

        if (Loadout == null || Anim == null) return false;
        if (!CanAttackNow()) return false;

        CurrentTarget = target;
        CurrentMode = Loadout.ChooseMode(target);
        CurrentRange = Loadout.GetRange(CurrentMode);

        if (Navigator != null) Navigator.Stop();

        Vector3 aimAt = AimPointWorld();
        FaceTarget(aimAt);            // rotate body yaw
        AimGunToward(aimAt, snap: true); // rotate gun right away (optional snap)

        Anim.SetTrigger(Loadout.GetTrigger(CurrentMode));

        IsAttacking = true;
        _hitDone = false;
        _attackStartTime = Time.time;
        _nextAttackAllowed = Time.time + Loadout.GetCooldown(CurrentMode);
        return true;
    }

    private Vector3 AimPointWorld()
    {
        // If you later add an IntruderAimPoint component, you can use it here.
        if (CurrentTarget == null) return transform.position + transform.forward * 5f;
        return CurrentTarget.transform.position + AimWorldOffset;
    }

    private void AimGunToward(Vector3 aimAt, bool snap = false)
    {
        if (AimPivot == null) return;

        Vector3 dir = aimAt - AimPivot.position;
        dir.y = dir.y; // keep pitch, don't flatten

        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion desired = Quaternion.LookRotation(dir.normalized, Vector3.up);

        if (snap)
            AimPivot.rotation = desired;
        else
            AimPivot.rotation = Quaternion.RotateTowards(AimPivot.rotation, desired, AimTurnSpeed * Time.deltaTime);
    }

    public void FaceTarget(Vector3 worldPos)
    {
        Vector3 look = worldPos - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
    }

    // Animation Event: impact frame for melee
    public void AE_AttackHit()
    {
        if (!IsAttacking || _hitDone) return;
        _hitDone = true;
        ApplyHit();
    }

    // Animation Event: fire frame for ranged
    public void AE_FireProjectile()
    {
        if (!IsAttacking) return;
        if (Loadout == null) return;
        if (CurrentMode != RobotCombatLoadout.AttackMode.Ranged) return;

        // Ensure we aim at the exact fire moment
        Vector3 aimAt = AimPointWorld();
        FaceTarget(aimAt);
        AimGunToward(aimAt, snap: true);

        SpawnProjectile();
        _hitDone = true;
    }

    // Animation Event: end frame
    public void AE_AttackEnd()
    {
        if (!IsAttacking) return;
        EndAttack();
    }

    private void ApplyHit()
    {
        if (CurrentTarget == null || Loadout == null) return;

        if (CurrentMode == RobotCombatLoadout.AttackMode.Ranged)
        {
            // If you’re not using AE_FireProjectile yet, this is what actually spawns bullets
            Vector3 aimAt = AimPointWorld();
            FaceTarget(aimAt);
            AimGunToward(aimAt, snap: true);

            SpawnProjectile();
            return;
        }

        // (your existing melee raycast logic can stay here)
        Vector3 hitOrigin = transform.TransformPoint(Loadout.MeleeHitOffset);
        Vector3 toTarget = CurrentTarget.transform.position - hitOrigin;

        float dist = toTarget.magnitude;
        if (dist > (Loadout.MeleeRange + Loadout.MeleeHitRadius)) return;

        if (Physics.Raycast(hitOrigin, toTarget.normalized, out var hit, dist, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(CurrentTarget.transform)) return;
        }

        var hp = CurrentTarget.GetComponent<Health>();
        if (hp != null && !hp.IsDead)
            hp.TakeDamage(Loadout.MeleeDamage, gameObject);
    }

    private Transform ResolveMuzzle()
    {
        if (MuzzleOverride != null) return MuzzleOverride;
        if (Loadout != null && Loadout.Muzzle != null) return Loadout.Muzzle;
        return null;
    }

    private void SpawnProjectile()
    {
        if (Loadout == null) return;

        var muzzle = ResolveMuzzle();
        if (Loadout.ProjectilePrefab == null || muzzle == null || CurrentTarget == null) return;

        Vector3 aimAt = AimPointWorld();
        Vector3 dir = (aimAt - muzzle.position);
        if (dir.sqrMagnitude < 0.0001f) dir = muzzle.forward;
        dir.Normalize();

        // Spawn projectile already rotated toward the target
        var proj = Instantiate(Loadout.ProjectilePrefab, muzzle.position, Quaternion.LookRotation(dir, Vector3.up));
        proj.GetComponent<RobotProjectile>().Init(gameObject, Loadout.RangedDamage, Loadout.ProjectileSpeed, dir);

        Log($"Fired projectile toward {CurrentTarget.name} dir={dir}");
    }

    private void EndAttack()
    {
        IsAttacking = false;
        CurrentTarget = null;
    }
}
