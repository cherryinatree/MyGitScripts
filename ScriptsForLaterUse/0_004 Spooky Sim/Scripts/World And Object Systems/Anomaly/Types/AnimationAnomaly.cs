using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Cherry.Anomalies
{
    public enum AnomalySpace { World, Local }

    [Serializable]
    public class AnimationAnomalyStep
    {
        [Tooltip("Seconds since anomaly activation.")]
        public float time = 0f;

        [Header("Movement / Pose")]
        public bool move = false;
        public Transform poseTarget;                 // If set, use this Transform's pose
        public Vector3 position;                    // Used if poseTarget is null
        public Vector3 eulerAngles;                 // Used if poseTarget is null
        public AnomalySpace space = AnomalySpace.World;
        [Min(0f)] public float moveDuration = 0f;   // 0 = teleport
        public AnimationCurve moveCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Animation")]
        public bool playAnimation = false;
        [Tooltip("Animator Trigger to set at this step.")]
        public string animatorTrigger;
        [Tooltip("Animator State to play at this step (optional).")]
        public string animatorStateName;
        [Min(0f)] public float crossFade = 0.1f;
        public int animatorLayer = 0;

        [Header("Audio")]
        public bool playAudio = false;
        public AudioClip audioClip;
        public bool oneShot = true;
        [Range(0f, 1f)] public float volume = 1f;

        [Header("VFX (optional)")]
        public bool playVFX = false;
        public ParticleSystem vfxPrefabOrReference;
        public Transform vfxAt;
        public bool vfxUseObjectTransformIfNull = true;
    }

    /// <summary>
    /// A mini-timeline anomaly:
    /// - Instantiates OR activates an object
    /// - Moves it to poses at times
    /// - Triggers animations at times
    /// - Plays audio at times
    /// - (Optional) VFX bursts
    /// </summary>
    public class AnimationAnomaly : AnomalyBase
    {
        [Header("Animated Object")]
        [Tooltip("If set and Instantiate On Activate = true, this prefab is spawned.")]
        [SerializeField] private GameObject prefab;

        [Tooltip("If you don't want to spawn, assign an existing scene object here.")]
        [SerializeField] private GameObject existingObject;

        [SerializeField] private bool instantiateOnActivate = true;
        [SerializeField] private Transform spawnParent;
        [SerializeField] private bool destroyInstancedOnDeactivate = true;
        [SerializeField] private bool restoreSceneObjectOnDeactivate = true;

        [Header("Sequence")]
        [SerializeField] private List<AnimationAnomalyStep> steps = new();
        [SerializeField] private bool autoResolveOnComplete = true;
        [SerializeField] private bool loopSequence = false;

        [Header("Components (optional overrides)")]
        [SerializeField] private Animator animatorOverride;
        [SerializeField] private AudioSource audioOverride;

        private GameObject runtimeObject;
        private Animator animator;
        private AudioSource audioSource;
        private Coroutine sequenceRoutine;

        // Cache original state if using an existing scene object
        private bool cached;
        private Vector3 normalWorldPos;
        private Quaternion normalWorldRot;
        private Vector3 normalLocalPos;
        private Quaternion normalLocalRot;
        private Vector3 normalLocalScale;
        private bool normalActive;

        protected override void Activate_Internal()
        {
            if (steps == null || steps.Count == 0)
            {
                Debug.LogWarning($"{name} AnimationAnomaly has no steps.");
                return;
            }

            PrepareRuntimeObject();
            PrepareComponents();

            // Start timeline
            sequenceRoutine = StartCoroutine(RunSequence());
        }

        protected override void Deactivate_Internal()
        {
            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
                sequenceRoutine = null;
            }

            if (audioSource) audioSource.Stop();

            // If we spawned it, clean it up
            if (runtimeObject && runtimeObject != existingObject)
            {
                if (destroyInstancedOnDeactivate)
                    Destroy(runtimeObject);
                else
                    runtimeObject.SetActive(false);
            }
            // If it's a scene object, restore baseline
            else if (runtimeObject == existingObject && restoreSceneObjectOnDeactivate)
            {
                RestoreExistingBaseline();
                runtimeObject.SetActive(normalActive);
            }

            runtimeObject = null;
            animator = null;
            audioSource = null;
        }

        protected override bool CheckResolved_Internal() => false;

        // --------------------------
        // Setup
        // --------------------------

        private void PrepareRuntimeObject()
        {
            if (instantiateOnActivate && prefab != null)
            {
                runtimeObject = Instantiate(prefab, spawnParent);
                runtimeObject.SetActive(true);
                cached = false; // spawned object doesn't get baseline restore
            }
            else
            {
                runtimeObject = existingObject != null ? existingObject : prefab;
                if (!runtimeObject)
                {
                    Debug.LogWarning($"{name} AnimationAnomaly missing prefab/existingObject.");
                    return;
                }

                CacheExistingBaselineIfNeeded(runtimeObject);
                runtimeObject.SetActive(true);
            }

            // Snap to first pose instantly if step[0] wants movement at time 0
            var first = steps.OrderBy(s => s.time).FirstOrDefault();
            if (first != null && first.move && first.time <= 0f)
            {
                ApplyPoseInstant(first);
            }
        }

        private void CacheExistingBaselineIfNeeded(GameObject obj)
        {
            if (cached || obj == null) return;

            normalWorldPos = obj.transform.position;
            normalWorldRot = obj.transform.rotation;
            normalLocalPos = obj.transform.localPosition;
            normalLocalRot = obj.transform.localRotation;
            normalLocalScale = obj.transform.localScale;
            normalActive = obj.activeSelf;

            cached = true;
        }

        private void RestoreExistingBaseline()
        {
            if (!cached || !existingObject) return;

            existingObject.transform.position = normalWorldPos;
            existingObject.transform.rotation = normalWorldRot;
            existingObject.transform.localPosition = normalLocalPos;
            existingObject.transform.localRotation = normalLocalRot;
            existingObject.transform.localScale = normalLocalScale;
        }

        private void PrepareComponents()
        {
            if (!runtimeObject) return;

            animator = animatorOverride ? animatorOverride : runtimeObject.GetComponentInChildren<Animator>(true);

            audioSource = audioOverride ? audioOverride : runtimeObject.GetComponentInChildren<AudioSource>(true);
            if (!audioSource)
                audioSource = runtimeObject.AddComponent<AudioSource>();
        }

        // --------------------------
        // Timeline Runner
        // --------------------------

        private IEnumerator RunSequence()
        {
            var ordered = steps.OrderBy(s => s.time).ToList();

            do
            {
                float start = Time.time;
                int index = 0;

                while (State == AnomalyState.Active && runtimeObject != null)
                {
                    float elapsed = Time.time - start;

                    // Execute all steps whose time has come (supports same-time bursts)
                    while (index < ordered.Count && elapsed >= ordered[index].time)
                    {
                        ExecuteStep(ordered[index]);
                        index++;
                    }

                    // If we reached the end, break or loop
                    if (index >= ordered.Count)
                        break;

                    yield return null;
                }

                if (State != AnomalyState.Active) yield break;

                if (loopSequence)
                {
                    // tiny pause before looping if you want
                    yield return null;
                }
                else
                {
                    break;
                }

            } while (loopSequence && State == AnomalyState.Active);

            if (autoResolveOnComplete && State == AnomalyState.Active)
                Resolve();
        }

        private void ExecuteStep(AnimationAnomalyStep step)
        {
            if (runtimeObject == null) return;

            if (step.move)
            {
                if (step.moveDuration <= 0f)
                    ApplyPoseInstant(step);
                else
                    StartCoroutine(ApplyPoseOverTime(step));
            }

            if (step.playAnimation)
                ApplyAnimation(step);

            if (step.playAudio)
                ApplyAudio(step);

            if (step.playVFX)
                ApplyVFX(step);
        }

        // --------------------------
        // Step Actions
        // --------------------------

        private void ApplyPoseInstant(AnimationAnomalyStep step)
        {
            var t = runtimeObject.transform;

            if (step.poseTarget != null)
            {
                if (step.space == AnomalySpace.World)
                {
                    t.position = step.poseTarget.position;
                    t.rotation = step.poseTarget.rotation;
                }
                else
                {
                    t.localPosition = step.poseTarget.localPosition;
                    t.localRotation = step.poseTarget.localRotation;
                }

                return;
            }

            if (step.space == AnomalySpace.World)
            {
                t.position = step.position;
                t.rotation = Quaternion.Euler(step.eulerAngles);
            }
            else
            {
                t.localPosition = step.position;
                t.localRotation = Quaternion.Euler(step.eulerAngles);
            }
        }

        private IEnumerator ApplyPoseOverTime(AnimationAnomalyStep step)
        {
            if (runtimeObject == null) yield break;

            Transform t = runtimeObject.transform;

            Vector3 startPos;
            Quaternion startRot;

            Vector3 endPos;
            Quaternion endRot;

            if (step.space == AnomalySpace.World)
            {
                startPos = t.position;
                startRot = t.rotation;
            }
            else
            {
                startPos = t.localPosition;
                startRot = t.localRotation;
            }

            if (step.poseTarget != null)
            {
                if (step.space == AnomalySpace.World)
                {
                    endPos = step.poseTarget.position;
                    endRot = step.poseTarget.rotation;
                }
                else
                {
                    endPos = step.poseTarget.localPosition;
                    endRot = step.poseTarget.localRotation;
                }
            }
            else
            {
                endPos = step.position;
                endRot = Quaternion.Euler(step.eulerAngles);
            }

            float dur = Mathf.Max(0.0001f, step.moveDuration);
            float startTime = Time.time;

            while (State == AnomalyState.Active && runtimeObject != null)
            {
                float u = (Time.time - startTime) / dur;
                if (u >= 1f) break;

                float eased = step.moveCurve != null ? step.moveCurve.Evaluate(u) : u;

                Vector3 p = Vector3.LerpUnclamped(startPos, endPos, eased);
                Quaternion r = Quaternion.SlerpUnclamped(startRot, endRot, eased);

                if (step.space == AnomalySpace.World)
                {
                    t.position = p;
                    t.rotation = r;
                }
                else
                {
                    t.localPosition = p;
                    t.localRotation = r;
                }

                yield return null;
            }

            if (runtimeObject != null)
            {
                if (step.space == AnomalySpace.World)
                {
                    t.position = endPos;
                    t.rotation = endRot;
                }
                else
                {
                    t.localPosition = endPos;
                    t.localRotation = endRot;
                }
            }
        }

        private void ApplyAnimation(AnimationAnomalyStep step)
        {
            if (!animator) return;

            if (!string.IsNullOrEmpty(step.animatorTrigger))
                animator.SetTrigger(step.animatorTrigger);

            if (!string.IsNullOrEmpty(step.animatorStateName))
            {
                if (step.crossFade > 0f)
                    animator.CrossFade(step.animatorStateName, step.crossFade, step.animatorLayer);
                else
                    animator.Play(step.animatorStateName, step.animatorLayer, 0f);
            }
        }

        private void ApplyAudio(AnimationAnomalyStep step)
        {
            if (!audioSource || step.audioClip == null) return;

            audioSource.volume = step.volume;

            if (step.oneShot)
                audioSource.PlayOneShot(step.audioClip, step.volume);
            else
            {
                audioSource.clip = step.audioClip;
                audioSource.loop = false;
                audioSource.Play();
            }
        }

        private void ApplyVFX(AnimationAnomalyStep step)
        {
            if (step.vfxPrefabOrReference == null) return;

            Transform at = step.vfxAt;

            if (at == null && step.vfxUseObjectTransformIfNull)
                at = runtimeObject.transform;

            if (at == null) return;

            // If the vfx is a scene reference already parented, just play it.
            if (step.vfxPrefabOrReference.gameObject.scene.IsValid())
            {
                step.vfxPrefabOrReference.transform.position = at.position;
                step.vfxPrefabOrReference.transform.rotation = at.rotation;
                step.vfxPrefabOrReference.Play(true);
            }
            else
            {
                // Otherwise treat as prefab
                var v = Instantiate(step.vfxPrefabOrReference, at.position, at.rotation);
                v.Play(true);
                Destroy(v.gameObject, v.main.duration + v.main.startLifetime.constantMax + 0.5f);
            }
        }
    }
}
