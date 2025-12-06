using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One mood's eye data: default open, a blink sequence, and named expressions.
/// Uses MouthSequence (your existing timeline asset) for blink/expressions.
/// </summary>
[CreateAssetMenu(fileName = "EyeSet", menuName = "Dialogue/Eye Set")]
public class EyeSet : ScriptableObject
{
    [Tooltip("Optional key (e.g., 'Neutral', 'Happy', 'Angry'). Used to pick mood by id.")]
    public string id = "Neutral";

    [Header("Default")]
    [Tooltip("Frame index in the animator's frames list for the open eyes.")]
    public int openFrameIndex = 0;

    [Header("Blink")]
    [Tooltip("Timeline for a blink (e.g., 0s=open, 0.05=half, 0.1=closed, 0.18=half, 0.22=open).")]
    public MouthSequence blinkSequence;

    [Header("Expressions")]
    public List<EyeExpression> expressions = new List<EyeExpression>();

    [Serializable]
    public struct EyeExpression
    {
        public string name;                     // e.g., "SmileEyes", "Worried", "Squint"
        public MouthSequence sequence;          // author the sequence in seconds -> frame indices
        public bool loop;                       // true for idle-looking loops like "soft squint"
        [Tooltip("If false, blinking will be paused while this expression is playing.")]
        public bool allowBlinkDuring;
    }

    public MouthSequence GetExpression(string exprName)
    {
        for (int i = 0; i < expressions.Count; i++)
            if (string.Equals(expressions[i].name, exprName, StringComparison.Ordinal))
                return expressions[i].sequence;
        return null;
    }

    public bool ExpressionLoops(string exprName)
    {
        for (int i = 0; i < expressions.Count; i++)
            if (string.Equals(expressions[i].name, exprName, StringComparison.Ordinal))
                return expressions[i].loop;
        return false;
    }

    public bool ExpressionAllowsBlink(string exprName)
    {
        for (int i = 0; i < expressions.Count; i++)
            if (string.Equals(expressions[i].name, exprName, StringComparison.Ordinal))
                return expressions[i].allowBlinkDuring;
        return true;
    }
}
