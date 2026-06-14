// DeliverCoordinatesToArtilleryOption.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Options/Deliver Coordinates To Artillery")]
public class DeliverCoordinatesToArtilleryOption : DialogueOption
{
    public override void ExecuteOption(CoreNPC npc)
    {
        if (string.IsNullOrEmpty(PlayerInventory.Instance.heldCoordinates))
        {
            Debug.Log("No coordinates to deliver.");
            return;
        }

        // hand off to Artillery action on this NPC
        var artillery = npc.GetComponent<ArtilleryNPC_Action>();
        if (artillery != null)
        {
            artillery.ReceiveFromPlayerAndFire(); // see action below
        }
        else
        {
            Debug.LogWarning("No ArtilleryNPC_Action on this NPC.");
        }
    }
}
