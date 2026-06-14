// AcknowledgeOption.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Options/Acknowledge")]
public class AcknowledgeOption : DialogueOption
{
    public override void ExecuteOption(CoreNPC npc)
    {
        // no-op; closes dialogue
    }
}
