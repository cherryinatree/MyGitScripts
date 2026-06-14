// DialogueLineCueDatabase.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Line Cue Database", fileName = "DialogueLineCueDatabase")]
public class DialogueLineCueDatabase : ScriptableObject
{
    [Serializable]
    public class LineCue
    {
        [Header("Lookup")]
        public string lineKey;          // must match what you raise (ex: "0")
        public string speakerId;        // optional filter (blank = any speaker)

        [Header("Mouth")]
        public string mouthSequenceId;  // DecalMouthAnimator library id (optional)

        [Header("Eyes")]
        public string moodId;           // DecalEyesAnimator mood id (optional)
        public string eyeExpression;    // expression name in current mood (optional)

        [Header("Character Animator")]
        public string animatorTrigger;  // optional
        public string animatorState;    // optional (plays/crossfades)
        [Min(0f)] public float crossFade = 0.1f;
    }

    public List<LineCue> cues = new List<LineCue>();

    // Runtime cache
    Dictionary<string, List<LineCue>> _byKey;

    void OnEnable() => BuildCache();

    void BuildCache()
    {
        _byKey = new Dictionary<string, List<LineCue>>(StringComparer.Ordinal);
        foreach (var c in cues)
        {
            if (c == null || string.IsNullOrEmpty(c.lineKey)) continue;

            if (!_byKey.TryGetValue(c.lineKey, out var list))
            {
                list = new List<LineCue>();
                _byKey.Add(c.lineKey, list);
            }
            list.Add(c);
        }
    }

    public bool TryGetCue(string lineKey, string speakerId, out LineCue cue)
    {
        cue = null;
        if (string.IsNullOrEmpty(lineKey)) return false;
        if (_byKey == null) BuildCache();

        if (!_byKey.TryGetValue(lineKey, out var list)) return false;

        // Prefer an exact speaker match, otherwise accept speaker-agnostic cue.
        for (int i = 0; i < list.Count; i++)
        {
            if (!string.IsNullOrEmpty(list[i].speakerId) &&
                string.Equals(list[i].speakerId, speakerId ?? "", StringComparison.Ordinal))
            {
                cue = list[i];
                return true;
            }
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (string.IsNullOrEmpty(list[i].speakerId))
            {
                cue = list[i];
                return true;
            }
        }

        return false;
    }
}
