using System.Collections;
using UnityEngine;

public class ComputerInteractable : Interactable
{



    [Header("Highlight (optional)")]
    [Tooltip("Optional highlight helper. If absent, no highlight is shown.")]
    public SimpleHighlighter highlighter;

    private SwitchControls switchControls;
    private SwitchCameras switchCameras;
    public GameObject computerScreen;
    public GameObject computerAI;

    AudioSource _audioSource;
    public AudioClip interactSound;


    private void Awake()
    {
        computerScreen.SetActive(false);
        _audioSource = GetComponent<AudioSource>();

        switchControls = FindFirstObjectByType<SwitchControls>();
        switchCameras = FindFirstObjectByType<SwitchCameras>();
    }

    public override bool CanInteract(GameObject interactor)
    {
        // Always interactable while enabled; you could add a 'locked' flag here.
        return base.CanInteract(interactor);
    }

    public override void Interact(GameObject interactor)
    {
        SwitchToComputer();
    }

    public override void OnFocusGained(GameObject interactor)
    {
        if (highlighter) highlighter.SetHighlighted(true);
    }

    public override void OnFocusLost(GameObject interactor)
    {
        if (highlighter) highlighter.SetHighlighted(false);
    }

    void OnPlayerInteract()
    {
        if (computerScreen.activeSelf)
        {
            SwitchToComputer();
        }
    }

    public void SwitchToComputer()
    {
        if (_audioSource != null && interactSound != null) _audioSource.PlayOneShot(interactSound);
        computerScreen.SetActive(!computerScreen.activeSelf);
        computerAI.SetActive(!computerAI.activeSelf);
        if (switchControls != null) switchControls.FlopScripts();
        if (switchCameras != null) switchCameras.FlopScripts();
    }

    public void SwitchToPlayer()
    {
        if (computerScreen.activeSelf)
        {
            SwitchToComputer();
        }
    }
}
