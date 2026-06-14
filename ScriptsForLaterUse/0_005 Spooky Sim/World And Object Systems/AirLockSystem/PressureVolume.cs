using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cherry.Airlocks
{
    [AddComponentMenu("Cherry/Airlocks/Pressure Volume")]
    [DisallowMultipleComponent]
    public class PressureVolume : MonoBehaviour
    {
        public enum PressureState { Pressurized, Depressurizing, Pressurizing, Vacuum }

        [Header("Volume")]
        [SerializeField] private Collider triggerVolume;

        [Header("Pressure (0 = vacuum, 1 = pressurized)")]
        [Range(0f, 1f)]
        [SerializeField] private float pressure01 = 1f;

        [SerializeField] private PressureState state = PressureState.Pressurized;

        [Header("Thresholds")]
        [Tooltip("Below this, we consider the volume vacuum.")]
        [SerializeField] private float vacuumThreshold = 0.02f;

        [Tooltip("Above this, we consider the volume pressurized.")]
        [SerializeField] private float pressurizedThreshold = 0.98f;

        public float Pressure01 => pressure01;

        public PressureState State
        {
            get => state;
            set
            {
                if (state == value) return;
                state = value;
                OnStateChanged?.Invoke(this, state);
            }
        }

        public event Action<PressureVolume, PressureState> OnStateChanged;

        private readonly HashSet<Suctionable> _suctionables = new();
        private readonly HashSet<Rigidbody> _rigidbodies = new();
        private readonly HashSet<PressureDoorLink> _doors = new();

        public IReadOnlyCollection<Suctionable> Suctionables => _suctionables;
        public IReadOnlyCollection<Rigidbody> Rigidbodies => _rigidbodies;
        public IReadOnlyCollection<PressureDoorLink> Doors => _doors;

        public bool IsVacuum => pressure01 <= vacuumThreshold;
        public bool IsPressurized => pressure01 >= pressurizedThreshold;

        private void Reset()
        {
            triggerVolume = GetComponent<Collider>();
            if (triggerVolume) triggerVolume.isTrigger = true;
        }

        private void Awake()
        {
            if (!triggerVolume) triggerVolume = GetComponent<Collider>();
            SyncStateFromPressure();
        }

        private void OnValidate()
        {
            pressure01 = Mathf.Clamp01(pressure01);
            SyncStateFromPressure();
            if (triggerVolume && !triggerVolume.isTrigger)
                triggerVolume.isTrigger = true;
        }

        internal void RegisterDoor(PressureDoorLink door)
        {
            if (door) _doors.Add(door);
        }

        internal void UnregisterDoor(PressureDoorLink door)
        {
            if (door) _doors.Remove(door);
        }

        public void ForcePressure01(float newPressure01)
        {
            pressure01 = Mathf.Clamp01(newPressure01);
            SyncStateFromPressure();
        }

        public void VentTowardsVacuum(float rate01PerSecond, float dt)
        {
            if (rate01PerSecond <= 0f || dt <= 0f) return;

            if (IsVacuum)
            {
                pressure01 = 0f;
                if (State != PressureState.Vacuum) State = PressureState.Vacuum;
                return;
            }

            pressure01 = Mathf.Max(0f, pressure01 - rate01PerSecond * dt);

            if (IsVacuum)
            {
                pressure01 = 0f;
                State = PressureState.Vacuum;
            }
            else
            {
                if (State != PressureState.Depressurizing) State = PressureState.Depressurizing;
            }
        }

        public void FillTowardsPressurized(float rate01PerSecond, float dt)
        {
            if (rate01PerSecond <= 0f || dt <= 0f) return;

            if (IsPressurized)
            {
                pressure01 = 1f;
                if (State != PressureState.Pressurized) State = PressureState.Pressurized;
                return;
            }

            pressure01 = Mathf.Min(1f, pressure01 + rate01PerSecond * dt);

            if (IsPressurized)
            {
                pressure01 = 1f;
                State = PressureState.Pressurized;
            }
            else
            {
                if (State != PressureState.Pressurizing) State = PressureState.Pressurizing;
            }
        }

        private void SyncStateFromPressure()
        {
            if (IsVacuum) state = PressureState.Vacuum;
            else if (IsPressurized) state = PressureState.Pressurized;
            else
            {
                // Keep current state if mid-transition, otherwise pick something reasonable
                if (state != PressureState.Depressurizing && state != PressureState.Pressurizing)
                    state = PressureState.Pressurized;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var s = other.GetComponentInParent<Suctionable>();
            if (s) _suctionables.Add(s);

            var rb = other.attachedRigidbody ? other.attachedRigidbody : other.GetComponentInParent<Rigidbody>();
            if (rb) _rigidbodies.Add(rb);
        }

        private void OnTriggerExit(Collider other)
        {
            var s = other.GetComponentInParent<Suctionable>();
            if (s) _suctionables.Remove(s);

            var rb = other.attachedRigidbody ? other.attachedRigidbody : other.GetComponentInParent<Rigidbody>();
            if (rb) _rigidbodies.Remove(rb);
        }
    }
}
