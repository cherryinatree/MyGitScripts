using System;
using System.Collections.Generic;
using UnityEngine;
using DialogueEditor;

[DisallowMultipleComponent]
public class DialogueCharacterActor : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("If blank, uses DefaultConversation.DefaultName. Must match the speaker id emitted by the conversation nodes.")]
    [SerializeField] private string speakerIdOverride;

    [Header("Conversations")]
    [SerializeField] private NPCConversation defaultConversation;
    [SerializeField] private List<NPCConversation> conversations = new List<NPCConversation>();

    [Header("Dialogue Binding")]
    [SerializeField] private DialogueEditorLineBinder lineBinder;

    [Header("Performance")]
    [SerializeField] private DecalMouthAnimator mouth;
    [SerializeField] private DecalEyesAnimator eyes;
    [SerializeField] private Animator animator;
    [SerializeField] private DialogueLineCueDatabase cueDatabase;

    [Header("Options")]
    [SerializeField] private bool autoFindMissingReferences = true;
    [SerializeField] private bool includeInactiveChildren = true;

    public string SpeakerId
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(speakerIdOverride))
                return speakerIdOverride;

            if (defaultConversation != null && !string.IsNullOrWhiteSpace(defaultConversation.DefaultName))
                return defaultConversation.DefaultName;

            return gameObject.name;
        }
    }

    public NPCConversation DefaultConversation => defaultConversation;

    void Reset()
    {
        AutoFindAll();
        EnsureDefaultConversationListed();
    }

    void Awake()
    {
        if (autoFindMissingReferences)
            AutoFindMissing();

        EnsureDefaultConversationListed();

        // Important: this actor now drives the mouth manually from dialogue events.
        if (mouth != null)
            mouth.autoPlayWhenAudioStarts = false;
    }

    void OnEnable()
    {
        DialogueLineSignals.OnLinePresented += OnLinePresented;
    }

    void OnDisable()
    {
        DialogueLineSignals.OnLinePresented -= OnLinePresented;
    }

    [ContextMenu("Auto Find References")]
    public void AutoFindAll()
    {
        lineBinder = GetComponent<DialogueEditorLineBinder>();
        defaultConversation = GetComponent<NPCConversation>();

        mouth = GetComponentInChildren<DecalMouthAnimator>(includeInactiveChildren);
        eyes = GetComponentInChildren<DecalEyesAnimator>(includeInactiveChildren);
        animator = GetComponentInChildren<Animator>(includeInactiveChildren);
    }

    void AutoFindMissing()
    {
        if (lineBinder == null) lineBinder = GetComponent<DialogueEditorLineBinder>();
        if (defaultConversation == null) defaultConversation = GetComponent<NPCConversation>();
        if (mouth == null) mouth = GetComponentInChildren<DecalMouthAnimator>(includeInactiveChildren);
        if (eyes == null) eyes = GetComponentInChildren<DecalEyesAnimator>(includeInactiveChildren);
        if (animator == null) animator = GetComponentInChildren<Animator>(includeInactiveChildren);
    }

    void EnsureDefaultConversationListed()
    {
        if (defaultConversation == null) return;
        if (!conversations.Contains(defaultConversation))
            conversations.Insert(0, defaultConversation);
    }

    public void PlayDefaultConversation()
    {
        StartConversation(defaultConversation);
    }

    public void PlayConversationByIndex(int index)
    {
        if (index < 0 || index >= conversations.Count)
        {
            Debug.LogWarning($"{name}: Conversation index {index} is out of range.");
            return;
        }

        StartConversation(conversations[index]);
    }

    public void StartConversation(NPCConversation conversation)
    {
        if (conversation == null)
        {
            Debug.LogWarning($"{name}: No conversation assigned.");
            return;
        }

        if (ConversationManager.Instance == null)
        {
            Debug.LogWarning($"{name}: No ConversationManager.Instance found.");
            return;
        }

        if (lineBinder == null)
            lineBinder = GetComponent<DialogueEditorLineBinder>();

        // Bind first so the first presented line is not missed.
        if (lineBinder != null)
            lineBinder.BindConversation(conversation);

        ConversationManager.Instance.StartConversation(conversation);
    }

    public void Advance()
    {
        if (ConversationManager.Instance == null) return;

        ConversationManager.Instance.SelectNextOption();
        ConversationManager.Instance.PressSelectedOption();
    }

    public void StopPerformance()
    {
        if (mouth != null) mouth.Stop();
        if (eyes != null) eyes.StopExpression();
    }

    void OnLinePresented(DialogueLineSignals.LineContext ctx)
    {
        if (!ShouldHandleLine(ctx.speakerId))
            return;

        DialogueLineCueDatabase.LineCue cue = null;
        if (cueDatabase != null)
            cueDatabase.TryGetCue(ctx.lineKey, ctx.speakerId, out cue);

        HandleMouth(ctx, cue);
        HandleEyes(cue);
        HandleAnimator(cue);
    }

    bool ShouldHandleLine(string incomingSpeakerId)
    {
        return string.Equals(incomingSpeakerId, SpeakerId, StringComparison.Ordinal);
    }

    void HandleMouth(DialogueLineSignals.LineContext ctx, DialogueLineCueDatabase.LineCue cue)
    {
        if (mouth == null) return;

        // Priority:
        // 1) explicit cue mouth sequence id
        // 2) match by clip
        // 3) fallback to line key
        if (cue != null && !string.IsNullOrEmpty(cue.mouthSequenceId))
        {
            mouth.PlayById(cue.mouthSequenceId, ctx.audio);
            return;
        }

        if (ctx.clip != null)
        {
            mouth.PlayByClip(ctx.clip, ctx.audio);
            return;
        }

        if (!string.IsNullOrEmpty(ctx.lineKey))
        {
            mouth.PlayById(ctx.lineKey, ctx.audio);
        }
    }

    void HandleEyes(DialogueLineCueDatabase.LineCue cue)
    {
        if (eyes == null || cue == null) return;

        if (!string.IsNullOrEmpty(cue.moodId))
            eyes.SetMood(cue.moodId);

        if (!string.IsNullOrEmpty(cue.eyeExpression))
            eyes.PlayExpression(cue.eyeExpression);
    }

    void HandleAnimator(DialogueLineCueDatabase.LineCue cue)
    {
        if (animator == null || cue == null) return;

        if (!string.IsNullOrEmpty(cue.animatorTrigger))
            animator.SetTrigger(cue.animatorTrigger);

        if (!string.IsNullOrEmpty(cue.animatorState))
            animator.CrossFadeInFixedTime(cue.animatorState, cue.crossFade, 0);
    }
}