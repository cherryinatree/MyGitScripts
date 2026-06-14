using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(menuName = "Cherry/Narrative/Sherlock Feed", fileName = "SherlockFeed_")]
public class SherlockFeedDefinition : ScriptableObject
{
    [Header("Identity")]
    public string feedId = "feed_id";
    public string displayTitle = "Sherlock Transmission";

    [Header("Content")]
    public VideoClip videoClip;
    [TextArea(2, 6)] public string subtitleText;

    [Header("Behavior")]
    public bool playOnce = true;
    public bool blockPlayerInput = true;
    public bool skippable = true;
    public float fallbackDuration = 4f;
    [Tooltip("Higher priority can interrupt lower priority feeds.")]
    public int priority = 0;

    [Header("Conditions")]
    public List<string> requiredFlags = new List<string>();

    [Header("Progression")]
    public List<string> setFlagsOnStart = new List<string>();
    public List<string> setFlagsOnComplete = new List<string>();
}