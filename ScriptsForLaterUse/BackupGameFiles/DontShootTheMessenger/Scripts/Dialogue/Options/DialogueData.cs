// DialogueData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueData", menuName = "Dialogue/DialogueData")]
public class DialogueData : ScriptableObject
{
    public string dialogueID;
    public DialogueCondition[] conditions;      // ALL must pass to use this dialogue
    public DialoguePage[] pages;
    [Tooltip("Higher priority wins when multiple dialogues pass conditions.")]
    public int priority = 0;

    public bool CheckCondition(CoreNPC npc)
    {
        if (conditions == null || conditions.Length == 0) return true;
        foreach (var c in conditions)
            if (c != null && c.Evaluate(npc) == false)
                return false;
        return true;
    }
}
