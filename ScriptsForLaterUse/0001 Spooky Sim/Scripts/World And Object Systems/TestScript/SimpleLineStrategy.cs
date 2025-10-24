using System.Collections;
using System.Linq;
using UnityEngine;
using ProcGen.Sockets;

public class SimpleLineStrategy : IRoomLayoutStrategy
{
    private readonly RoomSockets startPrefab;
    private readonly RoomSockets segmentPrefab;
    private readonly int length;

    public SimpleLineStrategy(RoomSockets start, RoomSockets segment, int length = 8)
    {
        this.startPrefab = start;
        this.segmentPrefab = segment;
        this.length = length;
    }

    public IEnumerator Execute(SocketSystem api)
    {
        // 1) Place the start room at origin
        var start = api.SpawnRoom(startPrefab, Vector3.zero, Quaternion.identity);

        // 2) Iteratively attach 'segmentPrefab' to the first free socket we find
        for (int i = 0; i < length; i++)
        {
            var targetSocket = api.AllFreeSockets().FirstOrDefault();
            if (targetSocket == null) yield break;

            bool ok = api.TryAttachRoomToSocket(
                segmentPrefab,
                targetSocket,
                chooseCandidateSocket: null,                  // just take the first compatible
                out var placed
            );

            // Optional pacing
            yield return null;

            if (!ok) break;
        }
    }
}
