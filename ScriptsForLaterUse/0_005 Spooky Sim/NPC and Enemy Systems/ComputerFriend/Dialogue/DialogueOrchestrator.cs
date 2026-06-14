// DialogueOrchestrator.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using DialogueEditor;

public class DialogueOrchestrator : MonoBehaviour
{
    public static DialogueOrchestrator Instance;

    [Header("Conversations")]
    public NPCConversation firstLine;
    public List<NPCConversation> conversations;

    [Header("Binder")]
    [SerializeField] private DialogueEditorLineBinder lineBinder;

    [Header("Actors (who should react to dialogue)")]
    public List<ActorRig> actors = new List<ActorRig>();

    [Serializable]
    public class ActorRig
    {
        [Tooltip("Must match NPCConversation.DefaultName (your log shows 'Sherlock').")]
        public string speakerId;

        public DecalMouthAnimator mouth;
        public DecalEyesAnimator eyes;
        public Animator animator;

        public DialogueLineCueDatabase cueDatabase; // optional but recommended
    }

    readonly Dictionary<string, ActorRig> _actorBySpeaker = new Dictionary<string, ActorRig>(StringComparer.Ordinal);

    void Awake()
    {
        Instance = this;

        // cache actors
        _actorBySpeaker.Clear();
        foreach (var a in actors)
        {
            if (a == null || string.IsNullOrEmpty(a.speakerId)) continue;
            _actorBySpeaker[a.speakerId] = a;

            // important: mouth should NOT fight you with auto mode
            if (a.mouth != null) a.mouth.autoPlayWhenAudioStarts = false;
        }
    }

    void OnEnable()
    {
        DialogueLineSignals.OnLinePresented += OnLinePresented;
    }

    void OnDisable()
    {
        DialogueLineSignals.OnLinePresented -= OnLinePresented;
    }

    // ---- Public API (everyone else calls these) ----

    public void StartConversation(NPCConversation convo)
    {
        if (convo == null) return;

        // Bind first so we never miss the first line event
        if (lineBinder == null) lineBinder = GetComponent<DialogueEditorLineBinder>();
        if (lineBinder != null) lineBinder.BindConversation(convo);

        ConversationManager.Instance.StartConversation(convo);
    }

    public void PlayOpeningLine()
    {
        StartConversation(firstLine);
    }

    public void StartConversationByID(int id)
    {
        if (id < 0 || id >= conversations.Count) return;
        StartConversation(conversations[id]);
    }

    public void Advance()
    {
        // This is what your DialougeInteract was doing
        ConversationManager.Instance.SelectNextOption();
        ConversationManager.Instance.PressSelectedOption();
    }

    // ---- Line handling (mouth/mood/eyes/anim) ----

    void OnLinePresented(DialogueLineSignals.LineContext ctx)
    {
        // ctx.speakerId is convo.DefaultName (your log: Sherlock)
        if (!_actorBySpeaker.TryGetValue(ctx.speakerId, out var actor) || actor == null)
            return;

        // 1) Look up a cue (optional)
        DialogueLineCueDatabase.LineCue cue = null;
        if (actor.cueDatabase != null)
            actor.cueDatabase.TryGetCue(ctx.lineKey, ctx.speakerId, out cue);

        // 2) Mouth (fallback order: cue id -> clip -> lineKey)
        if (actor.mouth != null)
        {
            if (cue != null && !string.IsNullOrEmpty(cue.mouthSequenceId))
                actor.mouth.PlayById(cue.mouthSequenceId, ctx.audio);
            else if (ctx.clip != null)
                actor.mouth.PlayByClip(ctx.clip, ctx.audio);
            else
                actor.mouth.PlayById(ctx.lineKey, ctx.audio); // works if your library ids match line keys
        }

        // 3) Eyes mood/expression
        if (actor.eyes != null && cue != null)
        {
            if (!string.IsNullOrEmpty(cue.moodId))
                actor.eyes.SetMood(cue.moodId);

            if (!string.IsNullOrEmpty(cue.eyeExpression))
                actor.eyes.PlayExpression(cue.eyeExpression);
        }

        // 4) Character animation
        if (actor.animator != null && cue != null)
        {
            if (!string.IsNullOrEmpty(cue.animatorTrigger))
                actor.animator.SetTrigger(cue.animatorTrigger);

            if (!string.IsNullOrEmpty(cue.animatorState))
                actor.animator.CrossFadeInFixedTime(cue.animatorState, cue.crossFade, 0);
        }
    }
}
