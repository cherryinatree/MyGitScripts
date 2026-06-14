using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Cherry.Puzzles
{
    [AddComponentMenu("Cherry/Puzzles/Pressure Platform")]
    [DisallowMultipleComponent]
    public class PressurePlatform : MonoBehaviour
    {
        [Header("Trigger Volume")]
        [Tooltip("Optional: if null, will use a Trigger Collider on this object.")]
        [SerializeField] private Collider triggerCollider;

        [Tooltip("Only these layers can press the platform. Leave empty (Everything) to allow all.")]
        [SerializeField] private LayerMask allowedLayers = ~0;

        [Tooltip("Optional: if set, only objects with this tag can press the platform.")]
        [SerializeField] private string requiredTag = "";

        [Header("Moving Part")]
        [Tooltip("The mesh/part that actually moves down/up (usually a child).")]
        [SerializeField] private Transform movingPart;

        [Tooltip("Local direction the moving part travels when pressed (e.g. (0,-1,0)).")]
        [SerializeField] private Vector3 localPressDirection = new Vector3(0f, -1f, 0f);

        [Tooltip("How far it moves along the press direction (in local units).")]
        [Min(0f)]
        [SerializeField] private float pressDistance = 0.08f;

        [Tooltip("Speed moving down.")]
        [Min(0f)]
        [SerializeField] private float pressSpeed = 6f;

        [Tooltip("Speed returning up.")]
        [Min(0f)]
        [SerializeField] private float releaseSpeed = 4f;

        [Header("State + Events")]
        [Tooltip("How many distinct objects must be on the platform to count as pressed.")]
        [Min(1)]
        [SerializeField] private int requiredPressCount = 1;

        [Tooltip("If an object gets destroyed without exiting, it will be auto-cleared after this timeout.")]
        [Min(0.05f)]
        [SerializeField] private float missingTimeoutSeconds = 0.35f;

        public UnityEvent OnPressed;
        public UnityEvent OnReleased;

        // keyId -> overlapCount (handles multi-collider characters/items)
        private readonly Dictionary<int, int> _overlapCounts = new();
        private readonly Dictionary<int, float> _lastSeenTime = new();

        private Vector3 _startLocalPos;
        private Vector3 _pressedLocalPos;
        private bool _isPressed;

        private void Reset()
        {
            triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null) triggerCollider.isTrigger = true;
            movingPart = transform;
        }

        private void Awake()
        {
            if (triggerCollider == null) triggerCollider = GetComponent<Collider>();
            if (triggerCollider == null || !triggerCollider.isTrigger)
            {
                Debug.LogError($"{name}: PressurePlatform needs a Trigger Collider (isTrigger = true).", this);
            }

            if (movingPart == null) movingPart = transform;

            _startLocalPos = movingPart.localPosition;

            var dir = localPressDirection.sqrMagnitude > 0.0001f ? localPressDirection.normalized : Vector3.down;
            _pressedLocalPos = _startLocalPos + dir * pressDistance;
        }

        private void Update()
        {
            CleanupMissingOverlaps();

            bool shouldBePressed = _overlapCounts.Count >= requiredPressCount;
            if (shouldBePressed != _isPressed)
            {
                _isPressed = shouldBePressed;
                if (_isPressed) OnPressed?.Invoke();
                else OnReleased?.Invoke();
            }

            // Move the platform visual
            Vector3 target = _isPressed ? _pressedLocalPos : _startLocalPos;
            float speed = _isPressed ? pressSpeed : releaseSpeed;

            movingPart.localPosition = Vector3.MoveTowards(
                movingPart.localPosition,
                target,
                speed * Time.deltaTime
            );
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsValidPresser(other)) return;

            int key = GetKey(other);
            if (_overlapCounts.TryGetValue(key, out int count)) _overlapCounts[key] = count + 1;
            else _overlapCounts[key] = 1;

            _lastSeenTime[key] = Time.time;
        }

        private void OnTriggerStay(Collider other)
        {
            if (!IsValidPresser(other)) return;

            int key = GetKey(other);
            // Even if counts got weird, stay means it's present.
            if (!_overlapCounts.ContainsKey(key)) _overlapCounts[key] = 1;
            _lastSeenTime[key] = Time.time;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsValidPresser(other)) return;

            int key = GetKey(other);
            if (_overlapCounts.TryGetValue(key, out int count))
            {
                count--;
                if (count <= 0)
                {
                    _overlapCounts.Remove(key);
                    _lastSeenTime.Remove(key);
                }
                else _overlapCounts[key] = count;
            }
        }

        private bool IsValidPresser(Collider other)
        {
            // Layer filter
            if (((1 << other.gameObject.layer) & allowedLayers.value) == 0)
                return false;

            // Tag filter
            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
                return false;

            return true;
        }

        private static int GetKey(Collider other)
        {
            // Prefer rigidbody identity, else root transform identity.
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null) return rb.GetInstanceID();
            return other.transform.root.GetInstanceID();
        }

        private void CleanupMissingOverlaps()
        {
            if (_lastSeenTime.Count == 0) return;

            float now = Time.time;
            // Copy keys to avoid modifying dictionary while iterating.
            _tempKeys.Clear();
            foreach (var kvp in _lastSeenTime)
                if (now - kvp.Value > missingTimeoutSeconds)
                    _tempKeys.Add(kvp.Key);

            for (int i = 0; i < _tempKeys.Count; i++)
            {
                int key = _tempKeys[i];
                _lastSeenTime.Remove(key);
                _overlapCounts.Remove(key);
            }
        }

        private static readonly List<int> _tempKeys = new();
    }
}