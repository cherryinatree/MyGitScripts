using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cherry.Airlocks
{
    [AddComponentMenu("Cherry/Airlocks/Airlock Controller")]
    public class AirlockController : MonoBehaviour
    {
        [Header("Airlock References")]
        [SerializeField] private PressureVolume airlockVolume;
        [SerializeField] private PressureDoorLink innerDoor; // airlock <-> ship room
        [SerializeField] private PressureDoorLink outerDoor; // airlock <-> vacuum (one side null)

        [Header("Manual Depressurization")]
        [SerializeField] private bool requireInnerDoorClosedToDepressurize = true;
        [SerializeField] private float depressurizeDuration = 2.0f;
        [SerializeField] private AudioSource depressurizeAudio;
        [SerializeField] private ParticleSystem depressurizeVfx;

        [Header("Auto Re-Pressurization (after OUTER door closes)")]
        [SerializeField] private bool autoRepressurizeOnOuterClose = true;
        [SerializeField] private bool requireBothDoorsClosedToRepressurize = true;
        [SerializeField] private float repressurizeDuration = 2.0f;
        [SerializeField] private AudioSource repressurizeAudio;
        [SerializeField] private ParticleSystem repressurizeVfx;

        [Header("Venting Model (fixes 'vacuum too fast')")]
        [Tooltip("How fast pressure goes down during venting. 1.0 means ~1 second from full to vacuum.")]
        [SerializeField] private float ventRate01PerSecond = 0.35f;

        [Tooltip("How fast pressure goes up during repressurize. 1.0 means ~1 second from vacuum to full.")]
        [SerializeField] private float fillRate01PerSecond = 0.35f;

        [Header("Suction Forces")]
        [SerializeField] private float suctionStrength = 35f;
        [SerializeField] private float suctionRadius = 20f;

        [Tooltip("Seconds for the initial blowdown ramp.")]
        [SerializeField] private float blowdownRampSeconds = 2.0f;

        [Tooltip("Curve during the initial ramp (0..1 time).")]
        [SerializeField] private AnimationCurve suctionRampCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Continuous Suction (Both Doors Open)")]
        [Tooltip("If true: when BOTH inner+outer are open, suction continues until either door closes.")]
        [SerializeField] private bool continuousSuctionWhenInnerAndOuterOpen = true;

        [Range(0f, 1f)]
        [SerializeField] private float bothOpenSustainMultiplier = 0.35f;

        [Header("Non-breach tail suction (Inner closed)")]
        [Tooltip("After ramp, with inner closed, keep a small tail suction while pressure is still venting.")]
        [Range(0f, 1f)]
        [SerializeField] private float innerClosedTailMultiplier = 0.15f;

        [Header("Vacuum SFX/VFX (plays when suction starts)")]
        [SerializeField] private AudioSource explosiveAudio;
        [SerializeField] private ParticleSystem explosiveVfx;

        [Header("Debug")]
        [SerializeField] private bool logEvents = false;

        private Coroutine _depressurizeRoutine;
        private Coroutine _repressurizeRoutine;
        private Coroutine _suctionRoutine;

        // Reusable collections (avoid GC)
        private readonly HashSet<PressureVolume> _visited = new();
        private readonly List<PressureVolume> _connectedVolumes = new(32);
        private readonly HashSet<Suctionable> _activeSuctionables = new();
        private readonly HashSet<Suctionable> _scratchSuctionables = new();

        private void Awake()
        {
            if (!airlockVolume) Debug.LogError($"{name}: AirlockController missing airlockVolume.", this);
            if (!outerDoor) Debug.LogError($"{name}: AirlockController missing outerDoor link.", this);

            HookDoor(innerDoor);
            HookDoor(outerDoor);

            OnAnyDoorStateChanged();
        }

        private void HookDoor(PressureDoorLink link)
        {
            if (!link || !link.DoorSignal) return;
            link.DoorSignal.OnStateChanged += (_, __) => OnAnyDoorStateChanged();
        }

        private void OnAnyDoorStateChanged()
        {
            EvaluateVacuumExposure();
            TryAutoRepressurize();
        }


        public void TogglePressure() 
        {             
            if (!airlockVolume) return;

            if (airlockVolume.IsPressurized)
                BeginDepressurize();
            else
                TryAutoRepressurize();
        }



        public void BeginDepressurize()
        {
            if (!airlockVolume) return;

            if (requireInnerDoorClosedToDepressurize && innerDoor && innerDoor.IsOpen)
            {
                if (logEvents) Debug.Log($"{name}: Depressurize blocked (inner door open).", this);
                return;
            }

            if (_repressurizeRoutine != null)
            {
                StopCoroutine(_repressurizeRoutine);
                _repressurizeRoutine = null;
            }

            if (_depressurizeRoutine != null)
            {
                StopCoroutine(_depressurizeRoutine);
                _depressurizeRoutine = null;
            }

            _depressurizeRoutine = StartCoroutine(DepressurizeRoutine());
        }

        private IEnumerator DepressurizeRoutine()
        {
            if (logEvents) Debug.Log($"{name}: Depressurizing airlock...", this);

            if (depressurizeVfx) depressurizeVfx.Play(true);
            if (depressurizeAudio) depressurizeAudio.Play();

            // Drive pressure down smoothly over time:
            float startP = airlockVolume ? airlockVolume.Pressure01 : 1f;
            float t = 0f;

            while (t < depressurizeDuration)
            {
                if (outerDoor && outerDoor.IsOpen) break;

                t += Time.deltaTime;
                float n = Mathf.Clamp01(t / Mathf.Max(0.01f, depressurizeDuration));
                float target = Mathf.Lerp(startP, 0f, n);
                airlockVolume.ForcePressure01(target);

                yield return null;
            }

            airlockVolume.ForcePressure01(0f);

            _depressurizeRoutine = null;
            EvaluateVacuumExposure();
        }

        private void TryAutoRepressurize()
        {
            if (!autoRepressurizeOnOuterClose) return;
            if (!airlockVolume || !outerDoor) return;

            if (outerDoor.IsOpen) return;
            if (airlockVolume.IsPressurized) return;

            if (requireBothDoorsClosedToRepressurize && innerDoor && innerDoor.IsOpen)
                return;

            if (_repressurizeRoutine != null) return;

            StopSuction();

            if (_depressurizeRoutine != null)
            {
                StopCoroutine(_depressurizeRoutine);
                _depressurizeRoutine = null;
            }

            _repressurizeRoutine = StartCoroutine(RepressurizeRoutine());
        }

        private IEnumerator RepressurizeRoutine()
        {
            if (logEvents) Debug.Log($"{name}: Re-pressurizing airlock...", this);

            if (repressurizeVfx) repressurizeVfx.Play(true);
            if (repressurizeAudio) repressurizeAudio.Play();

            float t = 0f;

            while (t < repressurizeDuration)
            {
                if (outerDoor && outerDoor.IsOpen)
                {
                    if (logEvents) Debug.Log($"{name}: Re-pressurize aborted (outer opened).", this);
                    _repressurizeRoutine = null;
                    EvaluateVacuumExposure();
                    yield break;
                }

                if (requireBothDoorsClosedToRepressurize && innerDoor && innerDoor.IsOpen)
                {
                    if (logEvents) Debug.Log($"{name}: Re-pressurize aborted (inner opened).", this);
                    _repressurizeRoutine = null;
                    yield break;
                }

                t += Time.deltaTime;

                // Smooth fill to 1 over duration (not instant)
                float n = Mathf.Clamp01(t / Mathf.Max(0.01f, repressurizeDuration));
                float target = Mathf.Lerp(airlockVolume.Pressure01, 1f, n);
                airlockVolume.ForcePressure01(target);

                yield return null;
            }

            airlockVolume.ForcePressure01(1f);
            _repressurizeRoutine = null;
        }

        private void EvaluateVacuumExposure()
        {
            if (!outerDoor || !outerDoor.IsOpen)
            {
                StopSuction();
                return;
            }

            bool bothOpen = outerDoor.IsOpen && innerDoor && innerDoor.IsOpen;

            // Start suction if:
            // - both doors open and continuous suction enabled (breach mode), OR
            // - there exists any connected volume with remaining pressure to vent.
            bool shouldStart =
                (bothOpen && continuousSuctionWhenInnerAndOuterOpen) ||
                AnyConnectedVolumeHasPressure();

            if (!shouldStart)
            {
                StopSuction();
                if (logEvents) Debug.Log($"{name}: Outer open but nothing to vent -> no suction.", this);
                return;
            }

            if (_suctionRoutine != null) return;

            if (logEvents) Debug.Log($"{name}: Starting suction (vacuum exposure).", this);

            if (explosiveVfx) explosiveVfx.Play(true);
            if (explosiveAudio) explosiveAudio.Play();

            if (_repressurizeRoutine != null)
            {
                StopCoroutine(_repressurizeRoutine);
                _repressurizeRoutine = null;
            }

            _suctionRoutine = StartCoroutine(SuctionRoutine());
        }

        private bool AnyConnectedVolumeHasPressure()
        {
            var start = outerDoor ? outerDoor.NonVacuumSide() : null;
            if (!start) return false;

            CollectReachableOpenVolumesNonAlloc(start, _connectedVolumes);

            for (int i = 0; i < _connectedVolumes.Count; i++)
            {
                var v = _connectedVolumes[i];
                if (!v) continue;
                if (v.Pressure01 > 0.02f) return true; // has something to vent
            }
            return false;
        }

        private void StopSuction()
        {
            if (_suctionRoutine == null) return;

            StopCoroutine(_suctionRoutine);
            _suctionRoutine = null;

            foreach (var s in _activeSuctionables)
                if (s) s.NotifySuctionEnd();

            _activeSuctionables.Clear();

            if (logEvents) Debug.Log($"{name}: Suction stopped.", this);
        }

        private IEnumerator SuctionRoutine()
        {
            float elapsed = 0f;

            while (outerDoor && outerDoor.IsOpen)
            {
                bool bothOpen = outerDoor.IsOpen && innerDoor && innerDoor.IsOpen;
                bool breachMode = bothOpen && continuousSuctionWhenInnerAndOuterOpen;

                var start = outerDoor.NonVacuumSide();
                if (!start) break;

                CollectReachableOpenVolumesNonAlloc(start, _connectedVolumes);

                // In non-breach mode, stop once everything is vented to vacuum.
                if (!breachMode)
                {
                    bool anyPressureLeft = false;
                    for (int i = 0; i < _connectedVolumes.Count; i++)
                    {
                        var v = _connectedVolumes[i];
                        if (!v) continue;
                        if (v.Pressure01 > 0.02f) { anyPressureLeft = true; break; }
                    }
                    if (!anyPressureLeft) break;
                }

                elapsed += Time.fixedDeltaTime;

                // Multiplier selection
                float mult;
                if (elapsed < blowdownRampSeconds)
                {
                    float n = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, blowdownRampSeconds));
                    mult = suctionRampCurve != null ? suctionRampCurve.Evaluate(n) : (1f - n);
                }
                else
                {
                    mult = breachMode ? bothOpenSustainMultiplier : innerClosedTailMultiplier;
                }

                // Build suctionable set
                _scratchSuctionables.Clear();
                for (int i = 0; i < _connectedVolumes.Count; i++)
                {
                    var v = _connectedVolumes[i];
                    if (!v) continue;
                    foreach (var s in v.Suctionables)
                        if (s) _scratchSuctionables.Add(s);
                }

                foreach (var s in _scratchSuctionables)
                {
                    if (_activeSuctionables.Add(s))
                        s.NotifySuctionStart();
                }

                if (_activeSuctionables.Count > 0)
                {
                    var toRemove = ListPool<Suctionable>.Get();
                    foreach (var s in _activeSuctionables)
                        if (!s || !_scratchSuctionables.Contains(s))
                            toRemove.Add(s);

                    for (int i = 0; i < toRemove.Count; i++)
                    {
                        var s = toRemove[i];
                        if (s) s.NotifySuctionEnd();
                        _activeSuctionables.Remove(s);
                    }
                    ListPool<Suctionable>.Release(toRemove);
                }

                // Vent pressure gradually (THIS is what prevents “vacuum too fast”)
                float ventRate = Mathf.Max(0f, ventRate01PerSecond) * Mathf.Max(0.05f, mult);
                for (int i = 0; i < _connectedVolumes.Count; i++)
                {
                    var v = _connectedVolumes[i];
                    if (!v) continue;
                    v.VentTowardsVacuum(ventRate, Time.fixedDeltaTime);
                }

                // Apply suction forces
                Vector3 pullPoint = outerDoor.SuctionPoint ? outerDoor.SuctionPoint.position : transform.position;

                foreach (var s in _activeSuctionables)
                {
                    if (!s) continue;

                    Vector3 to = pullPoint - s.transform.position;
                    float dist = to.magnitude;

                    float distMult = 1f;
                    if (suctionRadius > 0.01f)
                        distMult = Mathf.Clamp01(1f - (dist / suctionRadius));

                    float strength = suctionStrength * mult * Mathf.Lerp(0.2f, 1f, distMult);
                    s.ApplySuction(to, strength, Time.fixedDeltaTime);
                }

                yield return new WaitForFixedUpdate();
            }

            StopSuction();
            TryAutoRepressurize();
        }

        private void CollectReachableOpenVolumesNonAlloc(PressureVolume start, List<PressureVolume> results)
        {
            results.Clear();
            _visited.Clear();

            var q = QueuePool<PressureVolume>.Get();
            _visited.Add(start);
            q.Enqueue(start);

            while (q.Count > 0)
            {
                var v = q.Dequeue();
                if (!v) continue;

                results.Add(v);

                foreach (var door in v.Doors)
                {
                    if (!door || !door.IsOpen) continue;

                    var other = door.GetOther(v);
                    if (other && _visited.Add(other))
                        q.Enqueue(other);
                }
            }

            QueuePool<PressureVolume>.Release(q);
        }

        private static class QueuePool<T>
        {
            private static readonly Stack<Queue<T>> Pool = new();
            public static Queue<T> Get() => Pool.Count > 0 ? Pool.Pop() : new Queue<T>(32);
            public static void Release(Queue<T> q) { q.Clear(); Pool.Push(q); }
        }

        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> Pool = new();
            public static List<T> Get() => Pool.Count > 0 ? Pool.Pop() : new List<T>(32);
            public static void Release(List<T> l) { l.Clear(); Pool.Push(l); }
        }
    }
}
