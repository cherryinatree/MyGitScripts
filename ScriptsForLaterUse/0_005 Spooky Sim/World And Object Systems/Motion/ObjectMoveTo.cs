using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("Cherry/Movement/SimpleMover")]
public class ObjectMoveTo : MonoBehaviour
{
    [Header("Destination / Direction")]
    public Transform targetTransform;
    public Vector3 targetPosition;
    public Vector3 Direction = Vector3.forward;   // Used when useDirection = true

    [Tooltip("If true, use targetTransform; otherwise use targetPosition (or Direction if useDirection).")]
    public bool useTargetTransform = true;

    [Tooltip("If true, ignore target and move along Direction instead.")]
    public bool useDirection = false;

    [Header("Timing")]
    public bool moveOnStart = true;
    public bool moveAfterDelay = false;
    public float delaySeconds = 1f;
    public bool destroyOnComplete = false;
    // NOTE: If you have your own Timer type, trigger BeginMove() yourself when it fires.
    // public Timer delayTimer;

    [Header("Motion")]
    [Tooltip("If true, move using 'speed'. If false, complete in 'moveDuration' seconds.")]
    public bool moveBySpeed = true;
    public float speed = 1f;
    public float moveDuration = 1f;

    [Tooltip("When moving toward a target by speed, stop when within this distance.")]
    public float stoppingDistance = 0.01f;

    [Tooltip("If true and using a Transform target, keep following it as it moves (speed mode only).")]
    public bool followTarget = false;

    [Tooltip("Snap exactly to the final destination on complete.")]
    public bool snapOnComplete = true;

    [Header("Events")]
    public UnityEvent onMoveStart;
    public UnityEvent onMoveComplete;

    public bool IsMoving { get; private set; }

    Vector3 _startPos;
    Vector3 _destSnapshot;
    Coroutine _moveRoutine;

    void Start()
    {
        if (moveOnStart)
        {
            if (moveAfterDelay && delaySeconds > 0f)
                _moveRoutine = StartCoroutine(DelayThenBegin(delaySeconds));
            else
                BeginMove();
        }
    }

    IEnumerator DelayThenBegin(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        BeginMove();
    }


    /// <summary>Starts movement using current inspector settings.</summary>
    public void BeginMove()
    {
        StopMove();

        if (useDirection)
        {
            if (moveBySpeed) _moveRoutine = StartCoroutine(MoveDirectionBySpeed());
            else _moveRoutine = StartCoroutine(MoveDirectionByDuration());
        }
        else
        {
            if (useTargetTransform)
            {
                if (moveBySpeed) _moveRoutine = StartCoroutine(MoveToTargetBySpeed());
                else _moveRoutine = StartCoroutine(MoveToSnapshotByDuration(targetTransform.position));
            }
            else
            {
                if (moveBySpeed) _moveRoutine = StartCoroutine(MoveToPositionBySpeed(targetPosition));
                else _moveRoutine = StartCoroutine(MoveToSnapshotByDuration(targetPosition));
            }
        }
    }

    /// <summary>Stops movement immediately. Optionally snaps to final target if known.</summary>
    public void StopMove(bool snapToDestination = false)
    {
        if (_moveRoutine != null) StopCoroutine(_moveRoutine);
        _moveRoutine = null;
        IsMoving = false;

        if (snapToDestination && snapOnComplete)
        {
            if (useDirection && !moveBySpeed)
            {
                // Direction by duration snaps to start + Direction
                transform.position = _startPos + Direction;
            }
            else if (!useDirection && !moveBySpeed)
            {
                transform.position = _destSnapshot;
            }
            DestroyOnComplete();
        }
    }

    private void DestroyOnComplete()
    {
       if (destroyOnComplete)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>Set a new Transform target and (optionally) start moving.</summary>
    public void MoveTo(Transform newTarget, bool begin = true)
    {
        targetTransform = newTarget;
        useTargetTransform = true;
        useDirection = false;
        if (begin) BeginMove();
    }

    /// <summary>Set a new world position target and (optionally) start moving.</summary>
    public void MoveTo(Vector3 newPosition, bool begin = true)
    {
        targetPosition = newPosition;
        useTargetTransform = false;
        useDirection = false;
        if (begin) BeginMove();
    }

    /// <summary>Set a direction mode move (offset if duration, velocity if speed) and (optionally) start.</summary>
    public void MoveAlong(Vector3 direction, bool begin = true)
    {
        Direction = direction;
        useDirection = true;
        if (begin) BeginMove();
    }

    // -------- Coroutines --------

    IEnumerator MoveToTargetBySpeed()
    {
        if (targetTransform == null)
            yield break;

        IsMoving = true;
        onMoveStart?.Invoke();

        while (true)
        {
            Vector3 dest = followTarget ? targetTransform.position : targetTransform.position; // same but explicit
            Vector3 pos = transform.position;

            float step = Mathf.Max(0f, speed) * Time.deltaTime;
            Vector3 next = Vector3.MoveTowards(pos, dest, step);
            transform.position = next;

            if (!followTarget)
            {
                if (Vector3.SqrMagnitude(dest - next) <= stoppingDistance * stoppingDistance)
                {
                    if (snapOnComplete) transform.position = dest;
                    break;
                }
            }
            else
            {
                // Following a moving target: check proximity each frame.
                if (Vector3.SqrMagnitude(dest - next) <= stoppingDistance * stoppingDistance)
                {
                    // stay near it; keep following until caller stops OR target moves away again.
                    // If you want to auto-finish on first reach, uncomment the next lines:
                    // if (snapOnComplete) transform.position = dest;
                    // break;
                }
            }

            yield return null;
        }

        IsMoving = false;
        onMoveComplete?.Invoke();
    }

    IEnumerator MoveToPositionBySpeed(Vector3 dest)
    {
        IsMoving = true;
        onMoveStart?.Invoke();

        while (true)
        {
            Vector3 pos = transform.position;
            float step = Mathf.Max(0f, speed) * Time.deltaTime;
            Vector3 next = Vector3.MoveTowards(pos, dest, step);
            transform.position = next;

            if (Vector3.SqrMagnitude(dest - next) <= stoppingDistance * stoppingDistance)
            {
                if (snapOnComplete) transform.position = dest;
                break;
            }
            yield return null;
        }

        IsMoving = false;
        onMoveComplete?.Invoke();
    }

    IEnumerator MoveToSnapshotByDuration(Vector3 destSnapshot)
    {
        IsMoving = true;
        onMoveStart?.Invoke();

        _startPos = transform.position;
        _destSnapshot = destSnapshot;

        float dur = Mathf.Max(0.0001f, moveDuration);
        float t = 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            transform.position = Vector3.LerpUnclamped(_startPos, _destSnapshot, u);
            yield return null;
        }

        if (snapOnComplete) transform.position = _destSnapshot;

        IsMoving = false;
        onMoveComplete?.Invoke();
    }

    IEnumerator MoveDirectionBySpeed()
    {
        IsMoving = true;
        onMoveStart?.Invoke();

        Vector3 dirN = Direction.sqrMagnitude > 0f ? Direction.normalized : Vector3.zero;
        float remaining = moveDuration; // if <= 0, run indefinitely until StopMove is called

        if (remaining > 0f)
        {
            while (remaining > 0f)
            {
                float dt = Time.deltaTime;
                remaining -= dt;
                transform.position += dirN * Mathf.Max(0f, speed) * dt;
                yield return null;
            }
        }
        else
        {
            while (true)
            {
                transform.position += dirN * Mathf.Max(0f, speed) * Time.deltaTime;
                yield return null;
            }
        }

        IsMoving = false;
        onMoveComplete?.Invoke();
    }

    IEnumerator MoveDirectionByDuration()
    {
        IsMoving = true;
        onMoveStart?.Invoke();

        _startPos = transform.position;
        float dur = Mathf.Max(0.0001f, moveDuration);
        float t = 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            transform.position = Vector3.LerpUnclamped(_startPos, _startPos + Direction, u);
            yield return null;
        }

        if (snapOnComplete) transform.position = _startPos + Direction;

        IsMoving = false;
        onMoveComplete?.Invoke();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);

        Vector3 from = Application.isPlaying ? _startPos : transform.position;

        if (useDirection)
        {
            Vector3 to = (Application.isPlaying && !moveBySpeed) ? (_startPos + Direction) : (transform.position + Direction);
            Gizmos.DrawLine(transform.position, to);
            Gizmos.DrawWireSphere(to, 0.06f);
        }
        else
        {
            Vector3 dest = useTargetTransform && targetTransform ? targetTransform.position : targetPosition;
            Gizmos.DrawLine(transform.position, dest);
            Gizmos.DrawWireSphere(dest, 0.08f);
        }
    }
#endif
}
