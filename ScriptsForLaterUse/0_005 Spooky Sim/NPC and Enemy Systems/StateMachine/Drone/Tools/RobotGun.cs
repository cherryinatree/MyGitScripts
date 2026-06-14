using UnityEngine;

[DisallowMultipleComponent]
public class RobotGun : MonoBehaviour
{
    [Header("Refs")]
    public Transform Muzzle;                 // barrel tip
    public Transform AimPivot;              // gun root or hand bone (optional)
    public RobotProjectile ProjectilePrefab;

    [Header("Ballistics")]
    public float ProjectileSpeed = 40f;
    public float Damage = 10f;

    [Header("Fire Control")]
    [Tooltip("Seconds between shots. 0.1 = 10 shots/sec")]
    public float FireInterval = 0.1f;

    [Tooltip("Max distance to shoot.")]
    public float Range = 18f;

    [Header("Aiming")]
    public float AimTurnSpeed = 1440f;       // degrees/sec
    public Vector3 AimWorldOffset = new Vector3(0f, 1.2f, 0f); // aim at chest/head

    [Header("Accuracy (Calculator)")]
    [Range(0f, 1f)]
    [Tooltip("Base chance to shoot accurately (1 = always accurate, 0 = always miss).")]
    public float BaseAccuracy = 0.85f;

    [Tooltip("Accuracy decreases by this amount per meter of distance.")]
    public float AccuracyFalloffPerMeter = 0.01f;

    [Tooltip("Small cone applied even on accurate shots (degrees).")]
    public float HitConeDegrees = 1.5f;

    [Tooltip("Big cone applied on miss shots (degrees).")]
    public float MissConeDegrees = 12f;

    [Header("Sound")]
    public AudioSource Audio;
    public AudioClip ShootClip;
    [Range(0f, 1f)] public float ShootVolume = 0.8f;
    [Tooltip("Random pitch range for variation (1 = no variation).")]
    public Vector2 PitchRange = new Vector2(0.95f, 1.05f);

    [Header("Debug")]
    public bool DebugGun = false;

    private Intruder _target;
    private Health _targetHp;
    private bool _firing;
    private float _nextShotTime;
    private GameObject _owner;

    private void Awake()
    {
        // Ensure we have an AudioSource if a clip is assigned
        if (Audio == null && ShootClip != null)
        {
            Audio = gameObject.AddComponent<AudioSource>();
            Audio.playOnAwake = false;
            Audio.spatialBlend = 1f; // 3D sound
        }
    }

    public void StartFiring(GameObject owner, Intruder target)
    {
        _owner = owner;
        SetTarget(target);

        _firing = (_target != null);
        _nextShotTime = Time.time; // shoot immediately
    }

    public void StopFiring()
    {
        _firing = false;
        _target = null;
        _targetHp = null;
    }

    public void SetTarget(Intruder target)
    {
        _target = target;
        _targetHp = _target != null ? _target.GetComponent<Health>() : null;
    }

    private void LateUpdate()
    {
        if (!_firing) return;
        if (!IsTargetValid()) { StopFiring(); return; }

        AimAtTarget();
    }

    private void Update()
    {
        if (!_firing) return;
        if (!IsTargetValid()) { StopFiring(); return; }

        float dist = Vector3.Distance(GetMuzzlePos(), _target.transform.position);
        if (dist > Range) return;

        if (Time.time >= _nextShotTime)
        {
            FireOnce(dist);
            _nextShotTime = Time.time + Mathf.Max(0.01f, FireInterval);
        }
    }

    private bool IsTargetValid()
    {
        if (_target == null) return false;
        if (!_target.IsActive) return false;
        if (_targetHp != null && _targetHp.IsDead) return false;
        return true;
    }

    private Vector3 GetMuzzlePos() => (Muzzle != null) ? Muzzle.position : transform.position;

    private void AimAtTarget()
    {
        if (AimPivot == null) return;

        Vector3 aimAt = _target.transform.position + AimWorldOffset;
        Vector3 dir = aimAt - AimPivot.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion desired = Quaternion.LookRotation(dir.normalized, Vector3.up);
        AimPivot.rotation = Quaternion.RotateTowards(AimPivot.rotation, desired, AimTurnSpeed * Time.deltaTime);
    }

    private void FireOnce(float distanceToTarget)
    {
        if (ProjectilePrefab == null || Muzzle == null || _target == null) return;

        // Aim point (gun aims true, bullet may deviate)
        Vector3 aimAt = _target.transform.position + AimWorldOffset;

        Vector3 perfectDir = (aimAt - Muzzle.position);
        if (perfectDir.sqrMagnitude < 0.0001f) perfectDir = Muzzle.forward;
        perfectDir.Normalize();

        // ---- Accuracy calculator ----
        float accuracy = Mathf.Clamp01(BaseAccuracy - (distanceToTarget * AccuracyFalloffPerMeter));
        bool willMiss = Random.value > accuracy;

        float cone = willMiss ? MissConeDegrees : HitConeDegrees;
        Vector3 shotDir = RandomDirectionInCone(perfectDir, cone);

        // Spawn projectile
        var proj = Instantiate(ProjectilePrefab, Muzzle.position, Quaternion.LookRotation(shotDir, Vector3.up));
        proj.Init(_owner != null ? _owner : gameObject, Damage, ProjectileSpeed, shotDir);

        // Sound
        PlayShootSound();

        if (DebugGun)
            Debug.Log($"[RobotGun:{name}] Shot acc={accuracy:0.00} miss={willMiss} cone={cone} dist={distanceToTarget:0.0}");
    }

    private void PlayShootSound()
    {
        if (ShootClip == null) return;

        if (Audio == null)
        {
            Audio = gameObject.AddComponent<AudioSource>();
            Audio.playOnAwake = false;
            Audio.spatialBlend = 1f;
        }

        float pitch = Random.Range(PitchRange.x, PitchRange.y);
        Audio.pitch = pitch;
        Audio.PlayOneShot(ShootClip, ShootVolume);
    }

    // Uniform random direction within a cone around "forward"
    private static Vector3 RandomDirectionInCone(Vector3 forward, float coneAngleDeg)
    {
        if (coneAngleDeg <= 0.01f) return forward;

        float coneAngleRad = coneAngleDeg * Mathf.Deg2Rad;

        float z = Random.Range(Mathf.Cos(coneAngleRad), 1f);
        float theta = Random.Range(0f, Mathf.PI * 2f);
        float r = Mathf.Sqrt(1f - z * z);

        Vector3 local = new Vector3(r * Mathf.Cos(theta), r * Mathf.Sin(theta), z);

        Quaternion rot = Quaternion.FromToRotation(Vector3.forward, forward);
        return rot * local;
    }
}
