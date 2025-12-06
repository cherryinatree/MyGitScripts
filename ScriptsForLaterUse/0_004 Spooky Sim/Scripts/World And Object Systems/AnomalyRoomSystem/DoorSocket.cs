// DoorSocket.cs
using UnityEngine;

public enum SocketType { Entrance, Exit }

public class DoorSocket : MonoBehaviour
{
    public SocketType type;

    private void OnDrawGizmos()
    {
        Gizmos.color = (type == SocketType.Exit) ? Color.cyan : Color.magenta;
        Gizmos.DrawSphere(transform.position, 0.1f);
        // forward arrow
        Gizmos.DrawRay(transform.position, transform.forward * 0.6f);
    }
}
