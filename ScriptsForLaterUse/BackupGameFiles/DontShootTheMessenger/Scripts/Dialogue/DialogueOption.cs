using UnityEngine;

public abstract class DialogueOption : ScriptableObject
{
    public string optionText;

    // Called when player selects this option
    public abstract void ExecuteOption(CoreNPC npc);
}
