using UnityEngine;

[AddComponentMenu("Cherry/Encounters/Boss Exit Activator")]
public class BossExitActivator : MonoBehaviour
{
    [Header("Main Exit Object")]
    [Tooltip("Optional. If assigned, this object will be activated when the exit opens.")]
    [SerializeField] private GameObject exitRoot;

    [Header("Activate / Deactivate Objects")]
    [SerializeField] private GameObject[] activateObjects;
    [SerializeField] private GameObject[] deactivateObjects;

    [Header("Enable / Disable Components")]
    [SerializeField] private Behaviour[] enableBehaviours;
    [SerializeField] private Behaviour[] disableBehaviours;
    [SerializeField] private Collider[] enableColliders;
    [SerializeField] private Collider[] disableColliders;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openTrigger = "Open";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip activateClip;

    [Header("Options")]
    [SerializeField] private bool activateOnlyOnce = true;
    [SerializeField] private bool activateExitRoot = true;

    private bool _activated;

    public bool IsActivated => _activated;

    private void Reset()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    public void ActivateExit()
    {
        if (_activated && activateOnlyOnce)
            return;

        _activated = true;

        if (activateExitRoot && exitRoot != null)
            exitRoot.SetActive(true);

        SetObjectsActive(activateObjects, true);
        SetObjectsActive(deactivateObjects, false);

        SetBehavioursEnabled(enableBehaviours, true);
        SetBehavioursEnabled(disableBehaviours, false);

        SetCollidersEnabled(enableColliders, true);
        SetCollidersEnabled(disableColliders, false);

        if (animator != null && !string.IsNullOrWhiteSpace(openTrigger))
            animator.SetTrigger(openTrigger);

        if (audioSource != null && activateClip != null)
            audioSource.PlayOneShot(activateClip);
    }

    public void DeactivateExit()
    {
        _activated = false;

        if (exitRoot != null)
            exitRoot.SetActive(false);

        SetObjectsActive(activateObjects, false);
        SetObjectsActive(deactivateObjects, true);

        SetBehavioursEnabled(enableBehaviours, false);
        SetBehavioursEnabled(disableBehaviours, true);

        SetCollidersEnabled(enableColliders, false);
        SetCollidersEnabled(disableColliders, true);
    }

    private void SetObjectsActive(GameObject[] objects, bool state)
    {
        if (objects == null) return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(state);
        }
    }

    private void SetBehavioursEnabled(Behaviour[] behaviours, bool state)
    {
        if (behaviours == null) return;

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null)
                behaviours[i].enabled = state;
        }
    }

    private void SetCollidersEnabled(Collider[] colliders, bool state)
    {
        if (colliders == null) return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = state;
        }
    }
}