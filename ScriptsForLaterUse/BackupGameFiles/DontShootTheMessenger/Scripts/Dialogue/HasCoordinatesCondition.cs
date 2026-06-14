// HasCoordinatesCondition.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Conditions/HasCoordinates")]
public class HasCoordinatesCondition : DialogueCondition
{
    public bool shouldHaveCoordinates = true; // true = requires having coords, false = requires not having coords

    public override bool Evaluate(CoreNPC npc)
    {
        bool has = !string.IsNullOrEmpty(PlayerInventory.Instance.heldCoordinates);
        return shouldHaveCoordinates ? has : !has;
    }
}
