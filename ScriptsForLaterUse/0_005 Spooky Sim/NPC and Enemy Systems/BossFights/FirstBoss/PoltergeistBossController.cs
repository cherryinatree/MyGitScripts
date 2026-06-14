using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BeamEnemy))]
public class PoltergeistBossController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BeamEnemy beamEnemy;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    [Header("Hit Reaction")]
    [SerializeField] private float hitStunDuration = 1f;
    [SerializeField] private float hitReactionCooldown = 2f;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private string hitTrigger = "Hit";

    [Header("Death")]
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private string deathTrigger = "Die";
    [SerializeField] private bool destroyAfterDeath = true;
    [SerializeField] private float destroyDelay = 2f;

    private float _nextHitReactionAllowedTime;
    private Coroutine _hitRoutine;
    private bool _dead;
    private bool _stunned;

    public bool IsDead => _dead;
    public bool IsStunned => _stunned;

    private void Reset()
    {
        beamEnemy = GetComponent<BeamEnemy>();
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (beamEnemy == null) beamEnemy = GetComponent<BeamEnemy>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        if (beamEnemy != null)
        {
            beamEnemy.BeamDamaged += OnBeamDamaged;
            beamEnemy.BeamKilled += OnBeamKilled;
        }
    }

    private void OnDisable()
    {
        if (beamEnemy != null)
        {
            beamEnemy.BeamDamaged -= OnBeamDamaged;
            beamEnemy.BeamKilled -= OnBeamKilled;
        }
    }

    private void OnBeamDamaged(float amount, BeamRayDefinition ray, Vector3 hitPoint, Transform beamOrigin)
    {
        if (_dead) return;

        // Damage can keep happening every frame from the beam,
        // but the stun/animation reaction has its own cooldown.
        if (Time.time < _nextHitReactionAllowedTime)
            return;

        _nextHitReactionAllowedTime = Time.time + hitReactionCooldown;

        if (_hitRoutine != null)
            StopCoroutine(_hitRoutine);

        _hitRoutine = StartCoroutine(HitReactionRoutine());
    }

    private IEnumerator HitReactionRoutine()
    {
        _stunned = true;

        // Interrupt current attack here if you have an attack state machine
        CancelCurrentAttack();

        if (animator && !string.IsNullOrWhiteSpace(hitTrigger))
            animator.SetTrigger(hitTrigger);

        if (audioSource && hitClip)
            audioSource.PlayOneShot(hitClip);

        yield return new WaitForSeconds(hitStunDuration);

        if (_dead) yield break;

        _stunned = false;

        // After being hit, quickly move to a different location
        DashToDifferentWaypoint();

        _hitRoutine = null;
    }

    private void OnBeamKilled(BeamRayDefinition ray, Vector3 hitPoint, Transform beamOrigin)
    {
        if (_dead) return;
        _dead = true;
        _stunned = false;

        CancelCurrentAttack();
        StopAllMovement();

        if (animator && !string.IsNullOrWhiteSpace(deathTrigger))
            animator.SetTrigger(deathTrigger);

        if (audioSource && deathClip)
            audioSource.PlayOneShot(deathClip);

        if (destroyAfterDeath)
            Destroy(gameObject, destroyDelay);
    }

    private void CancelCurrentAttack()
    {
        // Put your "stop chasing / stop attack lunge" logic here.
    }

    private void StopAllMovement()
    {
        // Put your movement stop logic here.
        // NavMeshAgent stop, rigidbody stop, state change, etc.
    }

    private void DashToDifferentWaypoint()
    {
        // Put your "pick new waypoint and rush there" logic here.
    }
}