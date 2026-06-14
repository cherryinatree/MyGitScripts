using ProcGen.Anomalies;
using ProcGen.Sockets;
using UnityEngine;

public class GeneratorTrigger : MonoBehaviour
{
    private RoomSockets hallway;
    private AnomalyRoomGenerator generator;
    private bool backTriggered = false; 

    private void Awake()
    {
        hallway = GetComponent<RoomSockets>();
        generator = FindFirstObjectByType<AnomalyRoomGenerator>();
    }

    public void ForwardTrigger()
    {
        Debug.Log("Forward Triggered");

        if (!generator || !hallway)
            return;
        if (!backTriggered)
        {
            generator.HallwayForwardTriggered(hallway);
        }
        else 
        { 
            backTriggered = false;

            generator.HallwayForwardTriggeredBackGuess(hallway);
        }
       // generator.HallwayForwardTriggered(hallway);
    }

    public void BackTrigger()
    {
        Debug.Log("Back Triggered");

        if (!generator || !hallway)
            return;

        backTriggered = true;
        generator.HallwayBackTriggered(hallway);
    }

    public void RoomEntered()
    {
        Debug.Log("Room Entered Triggered");
    }
}