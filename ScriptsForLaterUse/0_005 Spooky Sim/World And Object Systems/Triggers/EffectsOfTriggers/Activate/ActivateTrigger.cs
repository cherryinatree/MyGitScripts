using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("Cherry/Triggers/Activate Trigger")]
public class ActivateTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private bool oneShot = false;
    [SerializeField] private float cooldown = 0f;
    [SerializeField] private float delayBeforeActivation = 0f;

    [Header("Objects To Activate")]
    [SerializeField] private List<GameObject> objectsToActivate = new();

    [Header("Objects To Deactivate")]
    [SerializeField] private List<GameObject> objectsToDeactivate = new();

    [Header("Objects To Toggle")]
    [SerializeField] private List<GameObject> objectsToToggle = new();

    [Header("Components To Enable")]
    [SerializeField] private List<Behaviour> componentsToEnable = new();

    [Header("Components To Disable")]
    [SerializeField] private List<Behaviour> componentsToDisable = new();

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private List<AudioClip> sounds = new();
    [SerializeField] private bool playSoundOnActivate = true;
    [SerializeField] private bool randomizePitch = false;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("Particles")]
    [SerializeField] private List<ParticleSystem> particlesToPlay = new();

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string animatorTriggerName;

    [Header("Extra Events")]
    public UnityEvent onActivated;
    public UnityEvent onReset;

    private bool hasActivated;
    private bool isRunning;
    private float lastActivatedTime = -999f;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Main function your general trigger should call.
    /// </summary>
    public void Activate()
    {
        if (!CanActivate())
            return;

        StartCoroutine(ActivateRoutine());
    }

    /// <summary>
    /// Instantly activates, ignoring delay.
    /// Useful for testing or special cases.
    /// </summary>
    public void ActivateInstantly()
    {
        if (!CanActivate())
            return;

        RunActivation();
    }

    /// <summary>
    /// Plays a sound only.
    /// </summary>
    public void PlaySound()
    {
        if (audioSource == null)
        {
            Debug.LogWarning($"{name}: No AudioSource assigned.", this);
            return;
        }

        if (sounds == null || sounds.Count == 0)
        {
            Debug.LogWarning($"{name}: No sounds assigned.", this);
            return;
        }

        AudioClip clip = sounds[Random.Range(0, sounds.Count)];

        if (clip == null)
            return;

        float originalPitch = audioSource.pitch;

        if (randomizePitch)
            audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);

        audioSource.PlayOneShot(clip);

        audioSource.pitch = originalPitch;
    }

    /// <summary>
    /// Activates assigned objects only.
    /// </summary>
    public void ActivateObjects()
    {
        SetGameObjectsActive(objectsToActivate, true);
    }

    /// <summary>
    /// Deactivates assigned objects only.
    /// </summary>
    public void DeactivateObjects()
    {
        SetGameObjectsActive(objectsToDeactivate, false);
    }

    /// <summary>
    /// Toggles assigned objects only.
    /// </summary>
    public void ToggleObjects()
    {
        foreach (GameObject obj in objectsToToggle)
        {
            if (obj != null)
                obj.SetActive(!obj.activeSelf);
        }
    }

    /// <summary>
    /// Resets this trigger so it can be used again.
    /// Especially useful if One Shot is enabled.
    /// </summary>
    public void ResetTrigger()
    {
        hasActivated = false;
        isRunning = false;
        onReset?.Invoke();
    }

    private IEnumerator ActivateRoutine()
    {
        isRunning = true;
        hasActivated = true;
        lastActivatedTime = Time.time;

        if (delayBeforeActivation > 0f)
            yield return new WaitForSeconds(delayBeforeActivation);

        RunActivation();

        isRunning = false;
    }

    private void RunActivation()
    {
        hasActivated = true;
        lastActivatedTime = Time.time;

        if (playSoundOnActivate)
            PlaySound();

        SetGameObjectsActive(objectsToActivate, true);
        SetGameObjectsActive(objectsToDeactivate, false);
        ToggleObjects();

        SetBehavioursEnabled(componentsToEnable, true);
        SetBehavioursEnabled(componentsToDisable, false);

        PlayParticles();
        TriggerAnimator();

        onActivated?.Invoke();
    }

    private bool CanActivate()
    {
        if (isRunning)
            return false;

        if (oneShot && hasActivated)
            return false;

        if (cooldown > 0f && Time.time < lastActivatedTime + cooldown)
            return false;

        return true;
    }

    private void SetGameObjectsActive(List<GameObject> objects, bool active)
    {
        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }

    private void SetBehavioursEnabled(List<Behaviour> behaviours, bool enabled)
    {
        foreach (Behaviour behaviour in behaviours)
        {
            if (behaviour != null)
                behaviour.enabled = enabled;
        }
    }

    private void PlayParticles()
    {
        foreach (ParticleSystem particle in particlesToPlay)
        {
            if (particle != null)
                particle.Play();
        }
    }

    private void TriggerAnimator()
    {
        if (animator == null)
            return;

        if (string.IsNullOrWhiteSpace(animatorTriggerName))
            return;

        animator.SetTrigger(animatorTriggerName);
    }
}