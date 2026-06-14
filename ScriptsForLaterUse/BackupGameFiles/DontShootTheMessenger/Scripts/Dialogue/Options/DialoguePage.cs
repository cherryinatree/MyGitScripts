using UnityEngine;

[System.Serializable]
public class DialoguePage
{
    [TextArea]
    public string[] sentences;

    public AudioClip voiceClip;             // Optional voice acting for this page
    public DialogueOption[] options;        // Options appear only on the last page
}
