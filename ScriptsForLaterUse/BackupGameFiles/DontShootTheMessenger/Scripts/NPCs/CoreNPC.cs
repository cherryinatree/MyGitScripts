using UnityEngine;

public class CoreNPC : MonoBehaviour
{
    public string npcName;

    [Header("Dialogue Sets")]
    public DialogueData[] dialogues;

    private NPC_Action[] actions;

    private void Awake()
    {
        actions = GetComponents<NPC_Action>();
    }

    public void Interact()
    {
        DialogueData dialogueToUse = GetCurrentDialogue();
        if (dialogueToUse != null)
        {
            DialogueManager.Instance.StartDialogue(this, dialogueToUse);
        }

        foreach (var action in actions)
        {
            action.OnPlayerInteract();
        }
    }
    DialogueData GetCurrentDialogue()
    {
        DialogueData best = null;
        int bestPriority = int.MinValue;

        foreach (var d in dialogues)
        {
            if (d != null && d.CheckCondition(this) && d.priority >= bestPriority)
            {
                best = d;
                bestPriority = d.priority;
            }
        }
        return best;
    }
    public void TriggerActions(string triggerEvent)
    {
        foreach (var action in actions)
        {
            action.OnTrigger(triggerEvent);
        }
    }
}
