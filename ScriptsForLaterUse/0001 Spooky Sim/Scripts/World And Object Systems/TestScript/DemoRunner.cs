using ProcGen.Sockets;
using UnityEngine;

public class DemoRunner : MonoBehaviour
{
    public SocketSystem system;
    public RoomSockets startPrefab;
    public RoomSockets segmentPrefab;

    private void Start()
    {
        var strat = new SimpleLineStrategy(startPrefab, segmentPrefab, 10);
        system.RunStrategy(strat);
    }
}
