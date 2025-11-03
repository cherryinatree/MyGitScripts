using System.Collections;
using UnityEngine;

public class ComputerInteractable : Interactable
{



    [Header("Highlight (optional)")]
    [Tooltip("Optional highlight helper. If absent, no highlight is shown.")]
    public SimpleHighlighter highlighter;

    private SwitchControls switchControls;
    private SwitchCameras switchCameras;


    AudioSource _audioSource;
    public AudioClip interactSound;


    private void Awake()
    {
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

    public void SwitchToComputer()
    {
        if (switchControls != null) switchControls.FlopScripts();
        if (switchCameras != null) switchCameras.FlopScripts();
    }

  
}
