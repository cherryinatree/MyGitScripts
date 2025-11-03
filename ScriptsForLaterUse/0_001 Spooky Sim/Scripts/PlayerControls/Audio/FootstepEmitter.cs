using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FootstepEmitter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Where the feet contact roughly are. Used for ground checks & sound position.")]
    public Transform footOrigin;              // set to player root or a feet bone/empty
    public LayerMask groundMask = ~0;         // what counts as ground
    private Rigidbody _rb;

    [Header("Step Logic (distance-based)")]
    [Tooltip("Minimum planar speed before steps play (m/s).")]
    public float minSpeedForSteps = 0.8f;
    [Tooltip("Meters traveled per footstep (at any speed). Lower = faster step cadence.")]
    [Range(0.4f, 3f)] public float stepDistance = 1.7f;
    [Tooltip("Random +/- variance added to step distance each step.")]
    [Range(0f, 0.6f)] public float stepDistanceJitter = 0.15f;

    [Header("Grounding")]
    [Tooltip("Sphere cast radius for ground detection.")]
    public float groundProbeRadius = 0.22f;
    [Tooltip("How far down to probe from footOrigin.")]
    public float groundProbeDistance = 0.5f;

    [Header("Clips")]
    [Tooltip("Default generic footstep set if no surface override is found.")]
    public AudioClip[] defaultFootsteps;
    [Tooltip("Optional jump/land stingers.")]
    public AudioClip jumpClip;
    public AudioClip landClip;
    public float landMinImpactSpeed = 3f;

    [Header("Surface Overrides (by tag or PhysicMaterial name)")]
    public List<SurfaceEntry> surfaceOverrides = new();

    [System.Serializable]
    public class SurfaceEntry
    {
        public enum KeyType { Tag, PhysicMaterialName }
        public KeyType keyType = KeyType.Tag;
        public string key;            // e.g. "Wood" tag or "Concrete" PhysicMaterial name
        public AudioClip[] footstepClips;
        [Range(0.2f, 2f)] public float volumeScale = 1f;
    }

    [Header("Playback")]
    [Range(0f, 1f)] public float volume = 0.9f;
    [Tooltip("Random pitch range for subtle variation.")]
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("Animation Event Mode")]
    [Tooltip("If true, footsteps won't auto-fire. Call StepNow() from an animation event.")]
    public bool useAnimationEventsOnly = false;

    private float _accumulatedMeters;
    private float _currentStepGoal;   // the next target distance for a step (with jitter)
    private bool _grounded, _wasGrounded;
    private Collider _groundColCache;
    private int _lastPlayedIndex = -1;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (!footOrigin) footOrigin = transform;
        ResetStepGoal();
    }

    private void Update()
    {
        GroundCheck();

        if (!useAnimationEventsOnly)
        {
            float planarSpeed = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z).magnitude;

            if (_grounded && planarSpeed >= minSpeedForSteps)
            {
                _accumulatedMeters += planarSpeed * Time.deltaTime;
                if (_accumulatedMeters >= _currentStepGoal)
                {
                    StepNow();
                    _accumulatedMeters = 0f;
                    ResetStepGoal();
                }
            }
            else
            {
                // Reset accumulation if you stop or leave ground to avoid instant step when resuming
                _accumulatedMeters = 0f;
            }
        }

        // Jump / Land stingers
        if (_wasGrounded && !_grounded)
        {
            if (jumpClip) AudioManager.Instance?.PlaySFX(jumpClip, footOrigin.position, volume * 0.9f, 1f, spatial: true);
        }
        else if (!_wasGrounded && _grounded)
        {
            // Estimate impact speed using downward velocity
            float impact = Mathf.Abs(_rb.linearVelocity.y);
            if (impact >= landMinImpactSpeed && landClip)
                AudioManager.Instance?.PlaySFX(landClip, footOrigin.position, Mathf.Clamp01(volume * Mathf.InverseLerp(landMinImpactSpeed, landMinImpactSpeed * 2f, impact)), 1f, spatial: true);
        }

        _wasGrounded = _grounded;
    }

    public void StepNow()
    {
        var (clips, volScale) = ResolveFootstepSet(_groundColCache);
        var clip = PickRandomNonRepeating(clips);
        if (clip)
        {
            float pitch = Random.Range(pitchRange.x, pitchRange.y);
            AudioManager.Instance?.PlaySFX(clip, footOrigin.position, volume * volScale, pitch, spatial: true);
        }
    }

    private void ResetStepGoal()
    {
        _currentStepGoal = stepDistance + Random.Range(-stepDistanceJitter, stepDistanceJitter);
        _currentStepGoal = Mathf.Max(0.2f, _currentStepGoal);
    }

    private void GroundCheck()
    {
        _grounded = false;
        _groundColCache = null;

        Vector3 origin = footOrigin.position + Vector3.up * 0.05f;
        if (Physics.SphereCast(origin, groundProbeRadius, Vector3.down, out var hit, groundProbeDistance + 0.05f, groundMask, QueryTriggerInteraction.Ignore))
        {
            _grounded = true;
            _groundColCache = hit.collider;
        }
    }

    private (AudioClip[] clips, float volumeScale) ResolveFootstepSet(Collider ground)
    {
        // Highest priority: explicit FootstepSurface on the collider
        if (ground && ground.TryGetComponent<FootstepSurface>(out var fs) && fs.footstepClips != null && fs.footstepClips.Length > 0)
            return (fs.footstepClips, fs.volumeScale);

        // Next: surfaceOverrides lookup
        if (ground)
        {
            string tagName = ground.tag;
            string pmName = ground.sharedMaterial ? ground.sharedMaterial.name : null;

            foreach (var e in surfaceOverrides)
            {
                if (e.footstepClips == null || e.footstepClips.Length == 0) continue;
                if (e.keyType == SurfaceEntry.KeyType.Tag && !string.IsNullOrEmpty(e.key) && tagName == e.key)
                    return (e.footstepClips, e.volumeScale);
                if (e.keyType == SurfaceEntry.KeyType.PhysicMaterialName && !string.IsNullOrEmpty(e.key) && pmName == e.key)
                    return (e.footstepClips, e.volumeScale);
            }
        }

        // Fallback
        return (defaultFootsteps, 1f);
    }

    private AudioClip PickRandomNonRepeating(AudioClip[] set)
    {
        if (set == null || set.Length == 0) return null;
        if (set.Length == 1) { _lastPlayedIndex = 0; return set[0]; }

        int idx;
        do { idx = Random.Range(0, set.Length); } while (idx == _lastPlayedIndex);
        _lastPlayedIndex = idx;
        return set[idx];
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!footOrigin) footOrigin = transform;
        Gizmos.color = Color.cyan;
        Vector3 origin = footOrigin.position + Vector3.up * 0.05f;
        Gizmos.DrawWireSphere(origin + Vector3.down * groundProbeDistance, groundProbeRadius);
    }
#endif
}
