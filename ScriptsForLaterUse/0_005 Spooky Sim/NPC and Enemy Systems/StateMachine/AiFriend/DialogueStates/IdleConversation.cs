using DialogueEditor;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class IdleConversation : CombatAction
{

    public UnityEvent onArrivedAtItem;
    public List<NPCConversation> conversations;

    [Header("Line system")]
    public DialogueEditorLineBinder lineBinder; // drag the component here

    public override void OnEnterState()
    {
        base.OnEnterState();


        var convo = conversations[0];

        // BIND FIRST so we don't miss the first line event
        if (lineBinder == null) lineBinder = GetComponent<DialogueEditorLineBinder>();
        if (lineBinder != null) lineBinder.BindConversation(convo);

        // Start the conversation
        //ConversationManager.Instance.StartConversation(convo);

        DialogueOrchestrator.Instance.StartConversation(convo);
        // REMOVE THIS:
        // decalMouthAnimator.PlayById("0", null);
    }

    public override void PerformAction()
    {    

    }
}
