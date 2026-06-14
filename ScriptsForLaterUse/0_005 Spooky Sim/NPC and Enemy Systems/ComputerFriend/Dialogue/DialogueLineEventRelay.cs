// DialogueLineEventRelay.cs
using UnityEngine;

public class DialogueLineEventRelay : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string speakerId = "ComputerFriend";

    [Header("Optional (only if you want audio-timed lipsync)")]
    [SerializeField] private AudioSource voiceSource; // assign the AudioSource that plays the dialogue clip

    public void PresentLine(string lineKey)
    {
        // This calls the global signal system I gave you earlier:
        DialogueLineSignals.RaiseLinePresented(speakerId, lineKey, voiceSource);
    }
}
