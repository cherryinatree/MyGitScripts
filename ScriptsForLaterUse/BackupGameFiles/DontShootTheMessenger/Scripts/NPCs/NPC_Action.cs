using UnityEngine;

public abstract class NPC_Action : MonoBehaviour
{
    protected CoreNPC npc;

    protected virtual void Awake()
    {
        npc = GetComponent<CoreNPC>();
    }

    // Called when player interacts
    public abstract void OnPlayerInteract();

    // Called for specific NPC events
    public virtual void OnTrigger(string triggerEvent) { }

    // Called when player selects a dialogue option
    public virtual void OnDialogueChoice(string choiceID) { }

    // NEW: Called each round so NPCs can update internal intel
    public virtual void OnRoundStart(TrenchRoute[] trenches) { }
}
