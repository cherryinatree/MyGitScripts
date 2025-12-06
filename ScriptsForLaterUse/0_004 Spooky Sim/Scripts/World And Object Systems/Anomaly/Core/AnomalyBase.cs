using UnityEngine;
using System;

namespace Cherry.Anomalies
{
    public enum AnomalyState { Idle, Armed, Active, Resolved }
    public enum AnomalyType { Object, Lights, VFX, Sound, Animation, Puzzle, Decal, Lock, Creature }

    /// <summary>
    /// Base class for all anomalies.
    /// Derive and override Activate_Internal / Deactivate_Internal / CheckResolved_Internal.
    /// </summary>
    public abstract class AnomalyBase : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string anomalyId = "anomaly_id";
        [SerializeField] private string displayName = "Anomaly";

        [Header("Activation")]
        [SerializeField, Min(0f)] private float weight = 1f;
        [SerializeField] private bool canRepeat = true;
        [SerializeField] private bool startArmed = true;

        [Header("Room/Scope")]
        [SerializeField] private string roomId = "default_room";

        public string AnomalyId => anomalyId;
        public string DisplayName => displayName;
        public float Weight => weight;
        public bool CanRepeat => canRepeat;
        public string RoomId => roomId;

        public AnomalyState State { get; private set; } = AnomalyState.Idle;
        public AnomalyType Type { get; protected set; } = AnomalyType.Object;

        public event Action<AnomalyBase> OnActivated;
        public event Action<AnomalyBase> OnDeactivated;
        public event Action<AnomalyBase> OnResolved;

        protected AnomalyManager Manager;

        protected virtual void OnEnable()
        {
            Manager = AnomalyManager.Instance;
            if (Manager != null) Manager.Register(this);
            if (startArmed) Arm();
        }

        protected virtual void OnDisable()
        {
            if (Manager != null) Manager.Unregister(this);
        }

        public virtual void Arm()
        {
            if (State != AnomalyState.Idle && State != AnomalyState.Resolved) return;
            State = AnomalyState.Armed;
        }

        public void Activate()
        {
            if (State != AnomalyState.Armed) return;
            State = AnomalyState.Active;
            Activate_Internal();
            OnActivated?.Invoke(this);
        }

        public void Deactivate()
        {
            if (State != AnomalyState.Active) return;
            State = AnomalyState.Armed;
            Deactivate_Internal();
            OnDeactivated?.Invoke(this);
        }

        public void Resolve()
        {
            if (State == AnomalyState.Resolved) return;
            State = AnomalyState.Resolved;
            Deactivate_Internal();
            OnResolved?.Invoke(this);

            if (!canRepeat) Manager?.MarkCompleted(this);
        }

        private void Update()
        {
            if (State == AnomalyState.Active && CheckResolved_Internal())
                Resolve();
        }

        // --- Override points ---
        protected abstract void Activate_Internal();
        protected abstract void Deactivate_Internal();
        protected virtual bool CheckResolved_Internal() => false;
    }
}
