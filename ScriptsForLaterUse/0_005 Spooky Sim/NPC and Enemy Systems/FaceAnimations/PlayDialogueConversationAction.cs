using UnityEngine;
using DialogueEditor;

public class PlayDialogueConversationAction : CombatAction
{
    [Header("Target")]
    [SerializeField] private DialogueCharacterActor actor;

    [Header("Conversation")]
    [Tooltip("If assigned, this conversation is played directly.")]
    [SerializeField] private NPCConversation conversationOverride;

    [Tooltip("If Override is null and index >= 0, plays actor conversation by index.")]
    [SerializeField] private int conversationIndex = -1;

    [Header("Behavior")]
    [SerializeField] private bool triggerOncePerStateEntry = true;

    private bool _triggered;
    private CombatState _lastState;

    public override void PerformAction()
    {
        // Reset when the state changes.
        if (stateMachine != null && stateMachine.CurrentState != _lastState)
        {
            _lastState = stateMachine.CurrentState;
            _triggered = false;
        }

        if (triggerOncePerStateEntry && _triggered)
            return;

        if (actor == null)
            actor = GetComponentInParent<DialogueCharacterActor>();

        if (actor == null)
        {
            Debug.LogWarning($"{name}: No DialogueCharacterActor assigned.");
            _triggered = true;
            return;
        }

        if (conversationOverride != null)
            actor.StartConversation(conversationOverride);
        else if (conversationIndex >= 0)
            actor.PlayConversationByIndex(conversationIndex);
        else
            actor.PlayDefaultConversation();

        _triggered = true;
    }

    public override bool ActionInProgress => false;
}