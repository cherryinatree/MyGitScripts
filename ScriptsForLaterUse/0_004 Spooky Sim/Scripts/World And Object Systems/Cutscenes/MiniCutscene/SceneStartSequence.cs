// SceneStartSequence.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AI;
using UnityEngine.InputSystem; // uses the new Input System's PlayerInput

public class SceneStartSequence : MonoBehaviour
{
    [Header("Spawn")]
    [Tooltip("Where the player should appear/face at scene start.")]
    public Transform spawnPoint;
    public bool snapRotation = true;

    [Header("Freeze / Effects")]
    [Tooltip("Seconds to hold the player still while you play VFX/SFX/Timeline.")]
    [Min(0f)] public float freezeSeconds = 2f;

    [Header("Auto-walk")]
    [Tooltip("How far to walk forward after the freeze.")]
    [Min(0f)] public float walkDistance = 2f;
    [Tooltip("Speed of the auto-walk when not using NavMeshAgent.")]
    [Min(0.01f)] public float walkSpeed = 2f;
    [Tooltip("If a NavMeshAgent is present on the player, use it for the auto-walk.")]
    public bool useNavMeshAgentIfPresent = true;

    [Header("Control Gating")]
    [Tooltip("Disable PlayerInput during the sequence.")]
    public bool disablePlayerInput = true;
    [Tooltip("Optional: components to disable during the sequence (e.g., your camera look script).")]
    public List<Behaviour> additionalToDisable;
    [Tooltip("Optional: PlayerAction scripts that should stay enabled even during cutscene (e.g., Pause).")]
    public List<PlayerAction> keepActionsEnabled;

    [Header("Events")]
    public UnityEvent onSequenceBegin;
    public UnityEvent onFrozenBegin;
    public UnityEvent onFrozenEnd;
    public UnityEvent onWalkBegin;
    public UnityEvent onWalkEnd;
    public UnityEvent onSequenceComplete;

    // Internals
    PlayerInput _input;
    CharacterController _cc;
    NavMeshAgent _agent;
    Rigidbody _rb;

    readonly List<Behaviour> _disabledBehaviours = new();
    readonly List<PlayerAction> _disabledActions = new();
    bool _running;

    IEnumerator Start()
    {
        // Wait one frame so everything in the scene finishes its Awake/Start.
        yield return null;
        Run();
    }

    public void Run()
    {
        if (_running) StartCoroutine(RestartRoutine());
        else StartCoroutine(RunRoutine());
    }

    IEnumerator RestartRoutine() { StopAllCoroutines(); yield return RunRoutine(); }

    IEnumerator RunRoutine()
    {
        _running = true;
        CacheRefs();

        onSequenceBegin?.Invoke();

        // 1) Spawn / snap transform
        if (spawnPoint)
        {
            TeleportTo(spawnPoint.position, snapRotation ? spawnPoint.rotation : transform.rotation);
        }
        ZeroMotion();

        // 2) Gate controls
        GateControl(true);

        // 3) Freeze window for effects
        onFrozenBegin?.Invoke();
        if (freezeSeconds > 0f)
            yield return new WaitForSeconds(freezeSeconds);
        onFrozenEnd?.Invoke();

        // 4) Auto-walk forward
        onWalkBegin?.Invoke();
        yield return WalkForwardRoutine();
        onWalkEnd?.Invoke();

        // 5) Return control
        GateControl(false);
        onSequenceComplete?.Invoke();

        _running = false;
    }

    void CacheRefs()
    {
        _input = GetComponentInParent<PlayerInput>();
        _cc = GetComponent<CharacterController>();
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();
    }

    void TeleportTo(Vector3 pos, Quaternion rot)
    {
        bool ccWasEnabled = _cc && _cc.enabled;
        if (ccWasEnabled) _cc.enabled = false;

        transform.SetPositionAndRotation(pos, rot);

        if (ccWasEnabled) _cc.enabled = true;
        if (_agent) _agent.Warp(pos);
    }

    void ZeroMotion()
    {
        if (_rb)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
        if (_agent)
        {
            _agent.ResetPath();
            _agent.isStopped = true;
        }
    }

    void GateControl(bool disable)
    {
        if (disable)
        {
            if (disablePlayerInput && _input && _input.enabled)
            {
                _input.enabled = false;
                _disabledBehaviours.Add(_input);
            }

            // Disable all PlayerAction-derived scripts (your input-driven actions)
            var actions = GetComponentsInChildren<PlayerAction>(true);
            foreach (var a in actions)
            {
                if (keepActionsEnabled != null && keepActionsEnabled.Contains(a)) continue;
                if (a.enabled)
                {
                    a.enabled = false;
                    _disabledActions.Add(a);
                }
            }

            if (additionalToDisable != null)
            {
                foreach (var b in additionalToDisable)
                {
                    if (b && b.enabled)
                    {
                        b.enabled = false;
                        _disabledBehaviours.Add(b);
                    }
                }
            }
        }
        else
        {
            foreach (var b in _disabledBehaviours) if (b) b.enabled = true;
            _disabledBehaviours.Clear();

            foreach (var a in _disabledActions) if (a) a.enabled = true;
            _disabledActions.Clear();
        }
    }

    IEnumerator WalkForwardRoutine()
    {
        if (walkDistance <= 0f) yield break;

         Vector3 dir = (spawnPoint ? spawnPoint.forward : transform.forward).normalized;
         Vector3 start = transform.position;
         Vector3 target = start + dir * walkDistance;

         // Use NavMeshAgent if present/allowed
         if (useNavMeshAgentIfPresent && _agent)
         {
             _agent.isStopped = false;
             _agent.updateRotation = true;
             _agent.SetDestination(target);

             // wait until arrived
             while (_agent.pathPending ||
                    _agent.remainingDistance > (_agent.stoppingDistance + 0.05f))
             {
                 yield return null;
             }
             _agent.isStopped = true;
             yield break;
         }

         // Fallback: CharacterController or simple transform move
         float remaining = walkDistance;
         while (remaining > 0f)
        {
            Debug.Log("walking");
            float step = walkSpeed * Time.deltaTime;
             Vector3 delta = dir * Mathf.Min(step, remaining);
            delta *= -2;
             if (_cc && _cc.enabled) _cc.Move(delta);
             else transform.position += delta;


            Debug.Log("Position: " + transform.position + " | delta: " + delta);
            remaining -= delta.magnitude;
             yield return null;
         }
    }

    // Optional public method to skip (bind to a key/UI if you want)
    public void SkipToEnd()
    {
        if (!_running) return;
        StopAllCoroutines();
        GateControl(false);
        _running = false;
        onSequenceComplete?.Invoke();
    }
}
