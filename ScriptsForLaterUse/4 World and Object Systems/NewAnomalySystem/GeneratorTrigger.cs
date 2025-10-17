using ProcGen.Anomalies;
using ProcGen.Sockets;
using UnityEngine;

public class GeneratorTrigger : MonoBehaviour
{
    RoomSockets hallway;
    AnomalyRoomGenerator generator;
    private bool backTriggered = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hallway = GetComponent<RoomSockets>();
        generator = FindFirstObjectByType<AnomalyRoomGenerator>();
    }

    public void ForwardTrigger()
    {
        Debug.Log("Forward Triggered");
        if (!backTriggered)
        {
            generator.HallwayForwardTriggered(hallway);
            generator.SpawnForward(hallway);
        }else
       {
            backTriggered = false;
            generator.HallwayBackTriggered(hallway);
            generator.SpawnForward(hallway);
        }

    }
    public void BackTrigger()
    {
        Debug.Log("Back Triggered");
       // if (!backTriggered)
       // {
            backTriggered = true;
            generator.HallwayBackTriggered(hallway);
            generator.SpawnBackward(hallway);
        // }
        // else
        // {
        //  generator.HallwayForwardTriggered(hallway);
        // }
    }

    public void RoomEntered()
    {
        Debug.Log("Room Entered Triggered");
      // generator.OnRoomEntered(hallway);
    }
}
