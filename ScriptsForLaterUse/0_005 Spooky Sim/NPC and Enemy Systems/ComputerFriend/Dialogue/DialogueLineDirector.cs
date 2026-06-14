// DialogueLineDirector.cs
using UnityEngine;

public class DialogueLineDirector : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Only respond to lines with this speakerId (leave blank to respond to all).")]
    [SerializeField] private string speakerId = "ComputerFriend";

    [Header("Data")]
    [SerializeField] private DialogueLineCueDatabase cueDatabase;

    [Header("Targets")]
    [SerializeField] private DecalMouthAnimator mouth;
    [SerializeField] private DecalEyesAnimator eyes;
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private AudioSource fallbackVoiceSource;

    string _lastLineKey;

    void Reset()
    {
        mouth = GetComponent<DecalMouthAnimator>();
        eyes = GetComponent<DecalEyesAnimator>();
        characterAnimator = GetComponentInChildren<Animator>();
        fallbackVoiceSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        DialogueLineSignals.OnLinePresented += HandleLinePresented;
    }

    void OnDisable()
    {
        DialogueLineSignals.OnLinePresented -= HandleLinePresented;
    }

    void HandleLinePresented(DialogueLineSignals.LineContext ctx)
    {
        if (!string.IsNullOrEmpty(speakerId) && ctx.speakerId != speakerId) return;
        if (cueDatabase == null) return;

        // Prevent double-firing the same line if your UI refreshes text twice.
        if (!string.IsNullOrEmpty(_lastLineKey) && _lastLineKey == ctx.lineKey) return;
        _lastLineKey = ctx.lineKey;

        if (!cueDatabase.TryGetCue(ctx.lineKey, ctx.speakerId, out var cue) || cue == null)
            return;

        // 1) Mouth
        if (mouth != null && !string.IsNullOrEmpty(cue.mouthSequenceId))
        {
            var src = ctx.audio != null ? ctx.audio : fallbackVoiceSource;
            mouth.PlayById(cue.mouthSequenceId, src);
        }

        // 2) Mood
        if (eyes != null && !string.IsNullOrEmpty(cue.moodId))
            eyes.SetMood(cue.moodId);

        // 3) Eye expression
        if (eyes != null && !string.IsNullOrEmpty(cue.eyeExpression))
            eyes.PlayExpression(cue.eyeExpression);

        // 4) Character animation
        if (characterAnimator != null)
        {
            if (!string.IsNullOrEmpty(cue.animatorTrigger))
                characterAnimator.SetTrigger(cue.animatorTrigger);

            if (!string.IsNullOrEmpty(cue.animatorState))
                characterAnimator.CrossFadeInFixedTime(cue.animatorState, cue.crossFade, 0);
        }
        
        // inside HandleLinePresented(...)
        if (mouth != null)
        {
            // If your database uses mouthSequenceId, do that:
            if (!string.IsNullOrEmpty(cue.mouthSequenceId))
                mouth.PlayById(cue.mouthSequenceId, ctx.audio);

            // Or if you want "match by AudioClip name/clip" instead:
            else if (ctx.clip != null)
                mouth.PlayByClip(ctx.clip, ctx.audio);
        }

    }
}
