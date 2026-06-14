// SetFlagOption.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Options/SetFlag")]
public class SetFlagOption : DialogueOption
{
    public string flagToSet;

    public override void ExecuteOption(CoreNPC npc)
    {
        if (!string.IsNullOrEmpty(flagToSet))
            GameState.Instance.SetFlag(flagToSet);
    }
}
