using System;
using UnityEngine;

namespace Cherry.Airlocks
{
    [AddComponentMenu("Cherry/Airlocks/Door Signal")]
    public class DoorSignal : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private bool isOpen;

        public bool IsOpen => isOpen;

        public event Action<DoorSignal> OnOpened;
        public event Action<DoorSignal> OnClosed;
        public event Action<DoorSignal, bool> OnStateChanged;

        [Header("Auto Close")]
        [SerializeField] private bool autoCloseEnabled = false;

        [Min(0f)]
        [SerializeField] private float autoCloseDelaySeconds = 3f;

        [Tooltip("If true, auto-close will only request close when the door is still open and enabled.")]
        [SerializeField] private bool autoCloseOnlyIfStillOpen = true;

        /// <summary>
        /// Subscribe from your door controller: when fired, actually start the close animation/logic.
        /// </summary>
        public event Action<DoorSignal> OnAutoCloseRequested;
        public GeneralInteract generalInteract;

        private float _autoCloseAtTime = -1f;

        private void Update()
        {
            if (!autoCloseEnabled) return;
            if (_autoCloseAtTime < 0f) return;

            if (Time.time >= _autoCloseAtTime)
            {
                _autoCloseAtTime = -1f;

                if (!autoCloseOnlyIfStillOpen || isOpen)
                    generalInteract.onInteract.Invoke();
            }
        }
        public void Toggle()
        {
            SetOpen(!isOpen);
        }


        /// <summary>Call this from your door system when the door is fully open.</summary>
        public void ReportOpened() => SetOpen(true);

        /// <summary>Call this from your door system when the door is fully closed.</summary>
        public void ReportClosed() => SetOpen(false);

        public void SetOpen(bool open)
        {
            if (isOpen == open) return;

            isOpen = open;

            // Start/stop auto-close timer
            if (isOpen)
            {
                if (autoCloseEnabled && autoCloseDelaySeconds > 0f)
                    _autoCloseAtTime = Time.time + autoCloseDelaySeconds;
                else
                    _autoCloseAtTime = -1f;
            }
            else
            {
                _autoCloseAtTime = -1f;
            }

            OnStateChanged?.Invoke(this, isOpen);
            if (isOpen) OnOpened?.Invoke(this);
            else OnClosed?.Invoke(this);
        }

        // Optional: allow other scripts to trigger/refresh the timer (like "door used again")
        public void ResetAutoCloseTimer()
        {
            if (!autoCloseEnabled || !isOpen) return;
            _autoCloseAtTime = Time.time + autoCloseDelaySeconds;
        }
    }
}
