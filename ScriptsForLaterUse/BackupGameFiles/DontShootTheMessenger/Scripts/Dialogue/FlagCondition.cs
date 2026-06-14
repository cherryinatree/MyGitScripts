// FlagCondition.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Conditions/FlagCondition")]
public class FlagCondition : DialogueCondition
{
    public string requiredFlag;   // e.g., "FiredOnce"
    public bool mustBeSet = true; // true: flag must be present; false: must be absent

    public override bool Evaluate(CoreNPC npc)
    {
        bool present = GameState.Instance.HasFlag(requiredFlag);
        return mustBeSet ? present : !present;
    }
}
