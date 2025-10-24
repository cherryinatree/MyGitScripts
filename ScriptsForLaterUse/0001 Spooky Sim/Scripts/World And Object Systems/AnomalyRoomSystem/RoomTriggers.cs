using UnityEngine;

public class RoomTriggers : MonoBehaviour
{
    private RoomTag hallway;

    public void Start()
    {

        hallway = GetComponent<RoomTag>();
    }
    public void Back()
    {

        CrossDir dir = CrossDir.TowardHallwayInterior;
        AnomalySystem.Instance?.OnHallwayGateCrossed(hallway, dir);
        AnomalySystem.Instance?.BacktrackIntoHallway(null);
    }

    public void Forward()
    {
        CrossDir dir = CrossDir.TowardRoom;
        AnomalySystem.Instance?.OnHallwayGateCrossed(hallway, dir);
        AnomalySystem.Instance?.CommitForwardFromCurrentRoom();
    }


}
