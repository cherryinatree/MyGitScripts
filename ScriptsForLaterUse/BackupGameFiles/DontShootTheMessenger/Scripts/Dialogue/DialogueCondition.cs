// DialogueCondition.cs
using UnityEngine;

public abstract class DialogueCondition : ScriptableObject
{
    public abstract bool Evaluate(CoreNPC npc);
}
