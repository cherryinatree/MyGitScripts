using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Items/Beam Pickup Path Mover")]
[DisallowMultipleComponent]
public class BeamPickupPathMover : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField, Min(0.01f)] private float speed = 8f;
    [SerializeField, Min(0.001f)] private float arriveDistance = 0.05f;
    [SerializeField] private bool faceMovement = false;

    private readonly List<Vector3> _path = new();
    private int _index;
    private Transform _finalTarget;
    private bool _active;

    /// <summary>
    /// returnPath should be ordered from pickup start (hit point) back toward player/origin.
    /// </summary>
    public void Begin(IReadOnlyList<Vector3> returnPath, Transform finalTarget)
    {
        _path.Clear();
        _index = 0;
        _finalTarget = finalTarget;
        _active = true;

        // Start at current position if it differs from the first point.
        Vector3 start = transform.position;

        if (returnPath != null && returnPath.Count > 0)
        {
            // Insert start if needed
            if ((returnPath[0] - start).sqrMagnitude > 0.0001f)
                _path.Add(start);

            // Copy with simple de-dup
            const float eps = 0.0005f;
            float eps2 = eps * eps;

            for (int i = 0; i < returnPath.Count; i++)
            {
                Vector3 p = returnPath[i];
                if (_path.Count == 0 || (_path[_path.Count - 1] - p).sqrMagnitude > eps2)
                    _path.Add(p);
            }
        }
        else
        {
            _path.Add(start);
        }

        // If we got basically no path, just go to final target.
        if (_path.Count < 2 && _finalTarget != null)
        {
            _path.Add(_finalTarget.position);
        }

        // Optional: disable any old straight-line mover to avoid fighting
        var legacy = GetComponent<PickupMover>(); // if you have one
        if (legacy != null) legacy.enabled = false;
    }

    private void Update()
    {
        if (!_active) return;

        Vector3 targetPos;

        // Follow waypoints first
        if (_index < _path.Count)
        {
            targetPos = _path[_index];
        }
        else if (_finalTarget != null)
        {
            // Dynamic final target so it still lands on the player if they move
            targetPos = _finalTarget.position;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Vector3 to = targetPos - transform.position;
        float dist = to.magnitude;

        if (dist <= arriveDistance)
        {
            _index++;
            // If we just finished the last waypoint, we’ll move to finalTarget next frame.
            if (_index >= _path.Count && _finalTarget == null)
            {
                Destroy(gameObject);
            }
            return;
        }

        Vector3 step = to / Mathf.Max(dist, 0.000001f);
        transform.position += step * (speed * Time.deltaTime);

        if (faceMovement && step.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(step, Vector3.up);
    }
}