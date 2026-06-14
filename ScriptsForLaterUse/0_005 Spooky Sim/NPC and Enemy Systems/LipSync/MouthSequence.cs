using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MouthSequence", menuName = "Dialogue/Mouth Sequence")]
public class MouthSequence : ScriptableObject
{
    [Serializable]
    public struct Step
    {
        [Min(0f)] public float time;     // seconds from start of playback
        public int frameIndex;           // which texture in your frames list
    }

    [Tooltip("Ordered by time (ascending). Each entry sets the mouth frame at or after 'time'.")]
    public List<Step> steps = new List<Step>();

    [Tooltip("If > 0, indicates the authored length of this sequence (helps with stretching).")]
    [Min(0f)] public float authoredLength = 0f;

    /// <summary>Returns the frame index that should be displayed at 't' seconds.</summary>
    public int FrameAt(float t)
    {
        if (steps == null || steps.Count == 0) return -1;
        // Find the last step whose time <= t
        int result = steps[0].frameIndex;
        for (int i = 1; i < steps.Count; i++)
        {
            if (steps[i].time <= t) result = steps[i].frameIndex;
            else break;
        }
        return result;
    }

    /// <summary>Returns last timestamp in steps if authoredLength == 0; otherwise authoredLength.</summary>
    public float EffectiveLength()
    {
        if (authoredLength > 0f) return authoredLength;
        if (steps == null || steps.Count == 0) return 0f;
        return steps[steps.Count - 1].time;
    }
}
