using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureAnimationController : MonoBehaviour
{
    public enum PlayType
    {
        TriggerParameter,
        CrossFadeState,
        PlayState,
        BoolPulse
    }

    [Serializable]
    public class CreatureAnimation
    {
        [Header("ID")]
        [Tooltip("Name used by other scripts to call this animation. Example: Roar, Bite, LookAround")]
        public string key;

        [Header("Animator")]
        public PlayType playType = PlayType.TriggerParameter;

        [Tooltip("Animator trigger/bool parameter name, or Animator state name depending on Play Type.")]
        public string animatorName;

        [Tooltip("Animator layer. Usually 0.")]
        public int layer = 0;

        [Header("Timing")]
        [Tooltip("Used by CrossFadeState.")]
        public float crossFadeDuration = 0.15f;

        [Tooltip("How long this animation should block random ambient animations.")]
        public float busyTime = 1f;

        [Tooltip("For BoolPulse only. How long the bool stays true.")]
        public float boolPulseTime = 0.2f;

        [Header("Rules")]
        [Tooltip("If false, this animation will not play while another animation is marked busy.")]
        public bool canInterruptBusyAnimation = true;
    }

    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animation List")]
    [SerializeField] private List<CreatureAnimation> animations = new();

    [Header("Random Ambient Animations")]
    [SerializeField] private bool useRandomAmbientAnimations = true;

    [Tooltip("These should match keys from the Animation List.")]
    [SerializeField] private List<string> randomAmbientAnimationKeys = new();

    [Tooltip("Minimum time after any called animation before random ambient animations are allowed.")]
    [SerializeField] private float minimumTimeSinceLastAnimation = 5f;

    [Tooltip("Minimum random wait before trying an ambient animation.")]
    [SerializeField] private float minAmbientDelay = 4f;

    [Tooltip("Maximum random wait before trying an ambient animation.")]
    [SerializeField] private float maxAmbientDelay = 9f;

    [Tooltip("If true, ambient animations will not play while the Animator is transitioning.")]
    [SerializeField] private bool avoidAmbientDuringAnimatorTransition = true;

    private readonly Dictionary<string, CreatureAnimation> animationLookup = new();

    private float lastAnimationTime = -999f;
    private float busyUntil = -999f;
    private float nextAmbientTime;
    private Coroutine boolPulseRoutine;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        BuildLookup();
        ScheduleNextAmbientAnimation();
    }

    private void Update()
    {
        HandleRandomAmbientAnimations();
    }

    private void BuildLookup()
    {
        animationLookup.Clear();

        foreach (CreatureAnimation anim in animations)
        {
            if (anim == null || string.IsNullOrWhiteSpace(anim.key))
                continue;

            if (!animationLookup.ContainsKey(anim.key))
            {
                animationLookup.Add(anim.key, anim);
            }
            else
            {
                Debug.LogWarning($"Duplicate creature animation key found: {anim.key}", this);
            }
        }
    }

    private void HandleRandomAmbientAnimations()
    {
        if (!useRandomAmbientAnimations)
            return;

        if (randomAmbientAnimationKeys.Count == 0)
            return;

        if (Time.time < nextAmbientTime)
            return;

        if (Time.time - lastAnimationTime < minimumTimeSinceLastAnimation)
        {
            ScheduleNextAmbientAnimation();
            return;
        }

        if (Time.time < busyUntil)
        {
            ScheduleNextAmbientAnimation();
            return;
        }

        if (avoidAmbientDuringAnimatorTransition && animator != null && animator.IsInTransition(0))
        {
            ScheduleNextAmbientAnimation();
            return;
        }

        string randomKey = randomAmbientAnimationKeys[UnityEngine.Random.Range(0, randomAmbientAnimationKeys.Count)];

        PlayAnimation(randomKey, false);

        ScheduleNextAmbientAnimation();
    }

    private void ScheduleNextAmbientAnimation()
    {
        nextAmbientTime = Time.time + UnityEngine.Random.Range(minAmbientDelay, maxAmbientDelay);
    }

    /// <summary>
    /// Call this from other scripts.
    /// Example:
    /// creatureAnimationController.PlayAnimation("Roar");
    /// </summary>
    public bool PlayAnimation(string key)
    {
        return PlayAnimation(key, true);
    }

    /// <summary>
    /// Call this from Unity Events because Unity Events like simple public methods.
    /// </summary>
    public void PlayAnimationFromEvent(string key)
    {
        PlayAnimation(key, true);
    }

    /// <summary>
    /// Force plays an animation, ignoring busy rules.
    /// Useful for deaths, attacks, possession events, jump scares, etc.
    /// </summary>
    public bool ForcePlayAnimation(string key)
    {
        return PlayAnimationInternal(key, true);
    }

    private bool PlayAnimation(string key, bool requestedByOutsideSource)
    {
        return PlayAnimationInternal(key, false);
    }

    private bool PlayAnimationInternal(string key, bool force)
    {
        if (animator == null)
        {
            Debug.LogWarning("No Animator assigned to CreatureAnimationController.", this);
            return false;
        }

        if (!animationLookup.TryGetValue(key, out CreatureAnimation anim))
        {
            Debug.LogWarning($"No creature animation found with key: {key}", this);
            return false;
        }

        if (!force && Time.time < busyUntil && !anim.canInterruptBusyAnimation)
            return false;

        if (string.IsNullOrWhiteSpace(anim.animatorName))
        {
            Debug.LogWarning($"Animation '{key}' has no Animator Name assigned.", this);
            return false;
        }

        switch (anim.playType)
        {
            case PlayType.TriggerParameter:
                animator.SetTrigger(anim.animatorName);
                break;

            case PlayType.CrossFadeState:
                animator.CrossFadeInFixedTime(
                    anim.animatorName,
                    anim.crossFadeDuration,
                    anim.layer
                );
                break;

            case PlayType.PlayState:
                animator.Play(anim.animatorName, anim.layer, 0f);
                break;

            case PlayType.BoolPulse:
                if (boolPulseRoutine != null)
                    StopCoroutine(boolPulseRoutine);

                boolPulseRoutine = StartCoroutine(BoolPulse(anim.animatorName, anim.boolPulseTime));
                break;
        }

        lastAnimationTime = Time.time;
        busyUntil = Time.time + anim.busyTime;

        return true;
    }

    private IEnumerator BoolPulse(string boolName, float pulseTime)
    {
        animator.SetBool(boolName, true);
        yield return new WaitForSeconds(pulseTime);
        animator.SetBool(boolName, false);
        boolPulseRoutine = null;
    }

    /// <summary>
    /// Useful if the creature dies, sleeps, gets frozen, etc.
    /// </summary>
    public void SetRandomAmbientAnimationsEnabled(bool enabled)
    {
        useRandomAmbientAnimations = enabled;

        if (enabled)
            ScheduleNextAmbientAnimation();
    }

    /// <summary>
    /// Useful after editing the animation list at runtime.
    /// Usually not needed.
    /// </summary>
    public void RefreshAnimationLookup()
    {
        BuildLookup();
    }
}