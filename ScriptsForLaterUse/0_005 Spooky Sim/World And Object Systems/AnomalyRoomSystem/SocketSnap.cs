// SocketSnap.cs
using UnityEngine;

public static class SocketSnap
{
    /// <summary>
    /// Snaps pieceRoot so that its entranceSocket aligns to targetExit:
    /// - entrance.position == targetExit.position
    /// - entrance.forward == -targetExit.forward
    /// Assumes sockets' +Z points "out" of their piece.
    /// </summary>
    public static void SnapEntranceToExit(Transform pieceRoot, Transform entranceSocket, Transform targetExit)
    {
        if (!pieceRoot || !entranceSocket || !targetExit) return;

        // 1) Rotate so entrance forward faces opposite of target exit forward
        Quaternion toOppose = Quaternion.FromToRotation(entranceSocket.forward, -targetExit.forward);
        pieceRoot.rotation = toOppose * pieceRoot.rotation;

        // 2) After rotation, move so entrance positions coincide
        Vector3 entranceWorldAfter = entranceSocket.position; // changes with rotation above
        Vector3 delta = targetExit.position - entranceWorldAfter;
        pieceRoot.position += delta;

        // Optional: align "up" more strictly if your art needs it (keeps floors level).
        // var alignUp = Quaternion.FromToRotation(entranceSocket.up, targetExit.up);
        // pieceRoot.rotation = alignUp * pieceRoot.rotation;
        // Recompute position afterward if you add this.
    }

    public static Transform FindSocket(Transform root, SocketType type)
    {
        foreach (var s in root.GetComponentsInChildren<DoorSocket>(true))
            if (s.type == type) return s.transform;
        Debug.LogError($"Socket '{type}' not found under {root.name}.");
        return null;
    }
}
