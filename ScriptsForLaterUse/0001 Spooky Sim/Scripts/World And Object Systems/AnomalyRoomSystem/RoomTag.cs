// RoomTag.cs
using UnityEngine;

public enum RoomKind { Start, Clean, Anomaly, End }

public class RoomTag : MonoBehaviour
{
    public RoomKind kind = RoomKind.Clean;

    public Transform ExitSocket => SocketSnap.FindSocket(transform, SocketType.Exit);
    public Transform EntranceSocket => SocketSnap.FindSocket(transform, SocketType.Entrance);

}
