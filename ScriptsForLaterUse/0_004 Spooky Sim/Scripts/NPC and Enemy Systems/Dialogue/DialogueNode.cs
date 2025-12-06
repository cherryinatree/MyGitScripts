// Assets/Dialogue/DialogueNode.cs
using UnityEngine;
using System.Collections.Generic;

public enum LineMood { Neutral, Happy, Sad, Angry, Surprised, Thinking, Anxious }

[System.Serializable]
public class DialogueLine
{
    [Tooltip("Match this ID to a speaker registered on DialogueRunner (e.g., \"AIHead\").")]
    public string speakerId = "AIHead";
    [TextArea] public string subtitle;
    public AudioClip voice;
    public LineMood mood = LineMood.Neutral;
    [Tooltip("If true and no audio, auto-advance after MinLineSeconds; otherwise wait for player input.")]
    public bool autoAdvance = true;
    [Range(0.1f, 10f)] public float minLineSeconds = 1.2f;
}

[System.Serializable]
public class DialogueChoice
{
    public string text;
    public DialogueNode next;
}

[CreateAssetMenu(menuName = "Dialogue/Node")]
public class DialogueNode : ScriptableObject
{
    [Header("Content")]
    public List<DialogueLine> lines = new List<DialogueLine>();

    [Header("Branching (optional)")]
    public List<DialogueChoice> choices = new List<DialogueChoice>();

    [Header("Fallback next if no choices")]
    public DialogueNode next;
}
