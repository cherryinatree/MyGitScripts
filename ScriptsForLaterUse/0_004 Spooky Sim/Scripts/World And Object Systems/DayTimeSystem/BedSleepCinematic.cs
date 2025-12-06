using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Unity.Cinemachine;

namespace Cherry.DayAndTime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cherry/Day And Time/Bed Sleep Cinematic")]
    public class BedSleepCinematic : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DayTimeSystem dayTime;
        [SerializeField] private ScreenFader fader;

        [Tooltip("The bed virtual camera that becomes active during the sleep cinematic.")]
        [SerializeField] private CinemachineVirtualCameraBase bedCamera;

        [Header("Optional player positioning")]
        [Tooltip("If set, moves player here before the camera blend (e.g. beside bed or on pillow).")]
        [SerializeField] private Transform movePlayerTo;
        [Tooltip("If set, moves player here after waking (e.g. beside bed).")]
        [SerializeField] private Transform wakePlayerTo;
        [SerializeField] private Transform playerRoot;

        [Header("Disable during sleep")]
        [SerializeField] private Behaviour[] disableWhileSleeping;

        [Header("Timings")]
        [SerializeField, Min(0f)] private float cameraBlendWait = 0.35f;
        [SerializeField, Min(0f)] private float closeEyesDuration = 0.6f;
        [SerializeField, Min(0f)] private float blackHoldDuration = 0.35f;
        [SerializeField, Min(0f)] private float openEyesDuration = 0.7f;

        [Header("Camera priority")]
        [SerializeField] private int bedCamPriorityDuring = 999;
        private int _bedCamPriorityOriginal;

        [Header("Events")]
        public UnityEvent onSleepDenied;
        public UnityEvent onSleepStarted;
        public UnityEvent onEyesClosed;
        public UnityEvent onWokeUp;

        private bool _playing;

        private void Awake()
        {
            if (dayTime == null) dayTime = DayTimeSystem.Instance;
            if (playerRoot == null)
            {
                // Best effort: find a tagged player root
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player) playerRoot = player.transform;
            }

            if (bedCamera != null)
                _bedCamPriorityOriginal = bedCamera.Priority;
        }

        /// <summary>Call this from your bed interact.</summary>
        public void TrySleep()
        {
            if (_playing) return;

            if (dayTime == null) dayTime = DayTimeSystem.Instance;
            if (dayTime == null || !dayTime.CanSleep)
            {
                onSleepDenied?.Invoke();
                return;
            }

            StartCoroutine(SleepRoutine());
        }

        private IEnumerator SleepRoutine()
        {
            _playing = true;

            onSleepStarted?.Invoke();

            // Disable controls/scripts while sleeping
            SetBehavioursEnabled(disableWhileSleeping, false);

            // Optional: move player into bed pose position (your animation system can also do this)
            if (playerRoot != null && movePlayerTo != null)
            {
                playerRoot.position = movePlayerTo.position;
                playerRoot.rotation = movePlayerTo.rotation;
            }

            // Switch to bed camera
            if (bedCamera != null)
            {
                _bedCamPriorityOriginal = bedCamera.Priority;
                bedCamera.Priority = bedCamPriorityDuring;
            }

            // Let Cinemachine blend settle a bit
            if (cameraBlendWait > 0f)
                yield return new WaitForSeconds(cameraBlendWait);

            // Close eyes (fade to black)
            if (fader != null)
                yield return fader.FadeToRoutine(1f, closeEyesDuration);

            onEyesClosed?.Invoke();

            if (blackHoldDuration > 0f)
                yield return new WaitForSeconds(blackHoldDuration);

            // Advance day here while screen is black
            dayTime.SleepToNextDay();

            // Optional: move player to wake spot (e.g. beside the bed)
            if (playerRoot != null && wakePlayerTo != null)
            {
                playerRoot.position = wakePlayerTo.position;
                playerRoot.rotation = wakePlayerTo.rotation;
            }

            // Return camera control to normal cameras (lower bed cam priority) while still black
            if (bedCamera != null)
                bedCamera.Priority = _bedCamPriorityOriginal;

            // Small hidden settle time (use your camera blend time if you want)
            if (cameraBlendWait > 0f)
                yield return new WaitForSeconds(cameraBlendWait);

            // Open eyes
            if (fader != null)
                yield return fader.FadeToRoutine(0f, openEyesDuration);

            // Re-enable scripts
            SetBehavioursEnabled(disableWhileSleeping, true);

            onWokeUp?.Invoke();
            _playing = false;
        }

        private static void SetBehavioursEnabled(Behaviour[] list, bool enabled)
        {
            if (list == null) return;
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] == null) continue;
                list[i].enabled = enabled;
            }
        }
    }
}
