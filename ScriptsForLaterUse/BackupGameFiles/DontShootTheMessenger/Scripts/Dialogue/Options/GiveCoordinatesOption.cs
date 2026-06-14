// GiveRoundCoordinatesOption.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Options/Give Round Coordinates")]
public class GiveRoundCoordinatesOption : DialogueOption
{
    public override void ExecuteOption(CoreNPC npc)
    {
        var round = GameManager.Instance.roundManager;
        string coords = round.GetCurrentTargetCoordinates();

        if (string.IsNullOrEmpty(PlayerInventory.Instance.heldCoordinates))
        {
            PlayerInventory.Instance.ReceiveCoordinates(coords);
            npc.TriggerActions("gave_coordinates"); // optional: animations, barks, etc.
        }
        else
        {
            Debug.Log("Player already holds coordinates.");
        }
    }
}