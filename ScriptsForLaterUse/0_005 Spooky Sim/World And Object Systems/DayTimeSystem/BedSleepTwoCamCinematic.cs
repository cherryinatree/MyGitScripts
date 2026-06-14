using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Unity.Cinemachine;

namespace Cherry.DayAndTime
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cherry/Day And Time/Bed Sleep Two-Cam Cinematic")]
    public class BedSleepTwoCamCinematic : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private DayTimeSystem dayTime;
        [SerializeField] private ScreenFader fader;

        [Tooltip("Optional; if empty we'll find one on Camera.main or in scene.")]
        [SerializeField] private CinemachineBrain brain;

        [Header("Bed Cameras (set both)")]
        [SerializeField] private CinemachineVirtualCameraBase sitCam;
        [SerializeField] private CinemachineVirtualCameraBase headCam;

        [Header("Priority During Cinematic")]
        [SerializeField] private int sitCamPriorityDuring = 900;
        [SerializeField] private int headCamPriorityDuring = 901;

        [Header("Timing")]
        [Tooltip("How long we stay on the sitting camera before switching to head cam.")]
        [SerializeField, Min(0f)] private float sitHoldSeconds = 0.9f;

        [Tooltip("How long we stay on head cam before closing eyes.")]
        [SerializeField, Min(0f)] private float headHoldSeconds = 0.4f;

        [SerializeField, Min(0f)] private float closeEyesDuration = 0.6f;
        [SerializeField, Min(0f)] private float blackHoldSeconds = 0.25f;
        [SerializeField, Min(0f)] private float openEyesDuration = 0.7f;

        [Header("Blend Waiting (recommended)")]
        [SerializeField] private bool waitForBlend = true;
        [SerializeField, Min(0.1f)] private float maxBlendWaitSeconds = 2.0f;

        [Header("Optional player placement")]
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Transform sitPose;
        [SerializeField] private Transform headPose;
        [SerializeField] private Transform wakePose;

        [Header("Disable while sleeping")]
        [SerializeField] private Behaviour[] disableWhileSleeping;

        [Header("Events")]
        public UnityEvent onSleepDenied;
        public UnityEvent onSleepStarted;
        public UnityEvent onSwitchedToSitCam;
        public UnityEvent onSwitchedToHeadCam;
        public UnityEvent onEyesClosed;
        public UnityEvent onWokeUp;

        private int _sitOriginalPriority;
        private int _headOriginalPriority;

        private bool _playing;

        private void Awake()
        {
            if (dayTime == null) dayTime = DayTimeSystem.Instance;

            if (playerRoot == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player) playerRoot = player.transform;
            }

            if (brain == null)
            {
                var cam = Camera.main;
                if (cam) brain = cam.GetComponent<CinemachineBrain>();
                if (brain == null) brain = FindFirstObjectByType<CinemachineBrain>();
            }

            if (sitCam != null) _sitOriginalPriority = sitCam.Priority;
            if (headCam != null) _headOriginalPriority = headCam.Priority;
        }

        public void TrySleep()
        {
            if (_playing) return;

            if (dayTime == null) dayTime = DayTimeSystem.Instance;
            if (dayTime == null || !dayTime.CanSleep)
            {
                onSleepDenied?.Invoke();
                return;
            }

            if (sitCam == null || headCam == null)
            {
                Debug.LogError($"{nameof(BedSleepTwoCamCinematic)} requires both SitCam and HeadCam assigned.", this);
                return;
            }

            StartCoroutine(Routine());
        }

        private IEnumerator Routine()
        {
            _playing = true;
            onSleepStarted?.Invoke();

            SetEnabled(disableWhileSleeping, false);

            // SIT phase
            if (playerRoot != null && sitPose != null) Snap(playerRoot, sitPose);
            PushBedCamPriorities(sitFirst: true);
            onSwitchedToSitCam?.Invoke();
            if (waitForBlend) yield return WaitBlend();

            if (sitHoldSeconds > 0f) yield return new WaitForSeconds(sitHoldSeconds);

            // HEAD phase
            if (playerRoot != null && headPose != null) Snap(playerRoot, headPose);
            PushBedCamPriorities(sitFirst: false);
            onSwitchedToHeadCam?.Invoke();
            if (waitForBlend) yield return WaitBlend();

            if (headHoldSeconds > 0f) yield return new WaitForSeconds(headHoldSeconds);

            // EYES CLOSE
            if (fader != null) yield return fader.FadeToRoutine(1f, closeEyesDuration);
            onEyesClosed?.Invoke();

            if (blackHoldSeconds > 0f) yield return new WaitForSeconds(blackHoldSeconds);

            // Advance day while black
            dayTime.SleepToNextDay();

            // Move to wake spot (still black, so pop is hidden)
            if (playerRoot != null && wakePose != null) Snap(playerRoot, wakePose);

            // Restore camera (still black)
            RestoreCamPriorities();
            if (waitForBlend) yield return WaitBlend();

            // EYES OPEN
            if (fader != null) yield return fader.FadeToRoutine(0f, openEyesDuration);

            SetEnabled(disableWhileSleeping, true);

            onWokeUp?.Invoke();
            _playing = false;
        }

        private void PushBedCamPriorities(bool sitFirst)
        {
            // Ensure whichever we want wins by having higher priority.
            if (sitFirst)
            {
                sitCam.Priority = sitCamPriorityDuring;
                headCam.Priority = headCamPriorityDuring - 2; // lower so sit is active
            }
            else
            {
                sitCam.Priority = sitCamPriorityDuring;
                headCam.Priority = headCamPriorityDuring;     // higher => head active
            }
        }

        private void RestoreCamPriorities()
        {
            if (sitCam != null) sitCam.Priority = _sitOriginalPriority;
            if (headCam != null) headCam.Priority = _headOriginalPriority;
        }

        private IEnumerator WaitBlend()
        {
            if (brain == null) yield break;

            // Wait a frame so the brain sees the priority change
            yield return null;

            float t = 0f;
            // CinemachineBrain.IsBlending exists in Cinemachine v2+.
            while (brain.IsBlending)
            {
                t += Time.deltaTime;
                if (t >= maxBlendWaitSeconds) break;
                yield return null;
            }
        }

        private static void Snap(Transform who, Transform to)
        {
            who.position = to.position;
            who.rotation = to.rotation;
        }

        private static void SetEnabled(Behaviour[] list, bool enabled)
        {
            if (list == null) return;
            foreach (var b in list) if (b != null) b.enabled = enabled;
        }
    }
}
