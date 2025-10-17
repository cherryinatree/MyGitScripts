// AnomalySystem.cs
using System.Collections.Generic;
using UnityEngine;

public class AnomalySystem : MonoBehaviour
{
    public static AnomalySystem Instance { get; private set; }

    [Header("Content")]
    public RoomSet roomSet;

    [Header("Rules")]
    [Range(0f, 1f)] public float anomalyChance = 0.35f;
    public bool useEndRoom = true;
    [Tooltip("After this many CLEAN rooms, spawn End Room instead of normal.")]
    public int cleanRoomsToEnd = 3;

    [Header("Spawn")]
    public Transform startAnchor;

    // Chain pointers:
    //   [startRoom] -> [currentHallway] -> [currentRoom] -> [nextHallwayAhead] -> [bufferRoomAhead]
    private RoomTag startRoom;
    private RoomTag currentHallway;
    private RoomTag currentRoom;        // spawned when approaching hallway gate
    private RoomTag nextHallwayAhead;   // spawned together with currentRoom
    private RoomTag bufferRoomAhead;    // spawned together with nextHallwayAhead (the "no-empty-door" buffer)

    private int cleanRoomsCrossed = 0;
    private bool pendingReroll = false; // set when backing out of Anomaly; applied on next approach

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (!roomSet || !roomSet.startRoom || !startAnchor)
        {
            Debug.LogError("[AnomalySystem] Assign RoomSet, startRoom, and startAnchor.");
            return;
        }

        // Spawn Start
        var startGO = Instantiate(roomSet.startRoom, startAnchor.position, startAnchor.rotation);
        startRoom = EnsureTag(startGO);
        startRoom.kind = RoomKind.Start;

        if (startRoom.ExitSocket == null)
        {
            Debug.LogError("[AnomalySystem] StartRoom missing ExitSocket.");
            return;
        }

        // Spawn first hallway
        currentHallway = SpawnNextAndSnap(RandomFrom(roomSet.hallwayPrefabs), startRoom.ExitSocket);
        if (!currentHallway || currentHallway.ExitSocket == null)
        {
            Debug.LogError("[AnomalySystem] Failed to spawn first hallway or missing ExitSocket.");
            return;
        }
        currentHallway.kind = RoomKind.Clean;

        // Front is built on approach
        ClearFront();
    }

    // ========== Hallway-only interface ==========
    public void OnHallwayGateCrossed(RoomTag hallway, CrossDir dir)
    {
        if (hallway == null || hallway != currentHallway) return;

        if (dir == CrossDir.TowardRoom)
        {
            BuildFrontIfNeeded();  // Spawn Room + Next Hallway + Buffer Room
        }
        else // TowardHallwayInterior (coming back from the room side)
        {
            // If current room is anomaly, mark reroll and nuke front immediately.
            if (currentRoom != null && currentRoom.kind == RoomKind.Anomaly)
            {
                pendingReroll = true;
                DestroyFront();
            }
            // If it was clean/end, do nothing; player just turned around.
        }
    }

    // Build or rebuild front chain at the current hallway end (called only while in hallway)
    private void BuildFrontIfNeeded()
    {
        if (!pendingReroll && currentRoom != null && nextHallwayAhead != null)
        {
            // Already have a front chain; ensure buffer exists
            if (bufferRoomAhead == null && currentRoom.kind != RoomKind.End)
            {
                bufferRoomAhead = SpawnRoomByRules_NoEndOverride(nextHallwayAhead.ExitSocket);
            }
            return;
        }

        DestroyFront();

        // Spawn Room A (apply "end" rule here)
        currentRoom = SpawnRoomByRules(currentHallway.ExitSocket);

        // If End spawned, no next hallway/room
        if (currentRoom == null || currentRoom.kind == RoomKind.End)
        {
            pendingReroll = false;
            return;
        }

        // Spawn Hallway B
        if (currentRoom.ExitSocket == null)
        {
            Debug.LogError("[AnomalySystem] currentRoom missing ExitSocket; cannot spawn next hallway.");
            pendingReroll = false;
            return;
        }
        nextHallwayAhead = SpawnNextAndSnap(RandomFrom(roomSet.hallwayPrefabs), currentRoom.ExitSocket);
        if (nextHallwayAhead != null) nextHallwayAhead.kind = RoomKind.Clean;

        // Spawn Buffer Room (Room B) beyond Hallway B
        if (nextHallwayAhead != null && nextHallwayAhead.ExitSocket != null)
        {
            bufferRoomAhead = SpawnRoomByRules_NoEndOverride(nextHallwayAhead.ExitSocket);
        }

        pendingReroll = false;
    }

    // ========== Progression when the player actually goes forward ==========
    // Call this from your door-open logic or when you decide "forward commit" (e.g., player crosses a tiny trigger placed just inside the room doorway).
    // If you truly want *no* triggers outside hallways, you can call this from an interaction (e.g., opening the door).
    public void CommitForwardFromCurrentRoom()
    {
        if (currentRoom == null) return;
        if (currentRoom.kind == RoomKind.Anomaly) return; // blocked

        // Success crossing a clean room
        if (currentRoom.kind == RoomKind.Clean) cleanRoomsCrossed++;

        // Advance pointers:
        startRoom = currentRoom;
        currentHallway = nextHallwayAhead;

        // Prepare to rebuild front at the *next* hallway gate
        currentRoom = null;
        nextHallwayAhead = null;
        bufferRoomAhead = null;
        pendingReroll = false;

        TryRecycleBehind();
    }

    // ========== Helpers ==========

    private RoomTag SpawnNextAndSnap(GameObject prefab, Transform attachExit)
    {
        if (!prefab || !attachExit) return null;

        var go = Instantiate(prefab);
        var tag = EnsureTag(go);

        var entrance = tag.EntranceSocket;
        if (entrance == null)
        {
            Debug.LogError($"[AnomalySystem] {go.name} missing EntranceSocket.");
            Destroy(go);
            return null;
        }

        go.transform.position = attachExit.position;
        go.transform.rotation = attachExit.rotation;
        SocketSnap.SnapEntranceToExit(go.transform, entrance, attachExit);
        return tag;
    }

    // Apply "end" rule
    private RoomTag SpawnRoomByRules(Transform attachExit)
    {
        if (useEndRoom && cleanRoomsCrossed >= cleanRoomsToEnd && roomSet.endRoom != null)
        {
            var end = SpawnNextAndSnap(roomSet.endRoom, attachExit);
            if (end != null) end.kind = RoomKind.End;
            return end;
        }
        return SpawnRoomByRules_NoEndOverride(attachExit);
    }

    // Spawn clean/anomaly but never End (used for buffer so you don't accidentally reveal the End too early)
    private RoomTag SpawnRoomByRules_NoEndOverride(Transform attachExit)
    {
        bool isAnomaly = Random.value < anomalyChance;
        if (isAnomaly)
        {
            var a = SpawnNextAndSnap(RandomFrom(roomSet.anomalyRoomPrefabs), attachExit);
            if (a != null) a.kind = RoomKind.Anomaly;
            return a;
        }
        else
        {
            var c = SpawnNextAndSnap(RandomFrom(roomSet.cleanRoomPrefabs), attachExit);
            if (c != null) c.kind = RoomKind.Clean;
            return c;
        }
    }

    private static GameObject RandomFrom(List<GameObject> list)
    {
        if (list == null || list.Count == 0) return null;
        return list[Random.Range(0, list.Count)];
    }

    private RoomTag EnsureTag(GameObject go)
    {
        var tag = go.GetComponent<RoomTag>();
        if (tag == null) tag = go.AddComponent<RoomTag>();
        return tag;
    }

    private void DestroyFront()
    {
        DestroyIfExists(bufferRoomAhead);
        DestroyIfExists(nextHallwayAhead);
        DestroyIfExists(currentRoom);
        currentRoom = null;
        nextHallwayAhead = null;
        bufferRoomAhead = null;
    }

    private void DestroyIfExists(RoomTag t)
    {
        if (t != null && t.gameObject) Destroy(t.gameObject);
    }

    private void ClearFront()
    {
        currentRoom = null;
        nextHallwayAhead = null;
        bufferRoomAhead = null;
        pendingReroll = false;
    }

    private void TryRecycleBehind()
    {
        // Optional: delete/pool segments N steps behind to save memory.
    }

    // Toggle: reroll immediately when you step back into the hallway, or wait until you approach the gate again
    [Header("Behavior")]
    public bool autoRebuildOnBacktrack = true;

    // Quick check for entrance trigger
    public bool IsCurrentRoomAnomaly()
    {
        return currentRoom != null && currentRoom.kind == RoomKind.Anomaly;
    }

    // Called by HallwayEntranceCommitTrigger when returning from an anomaly room
    public void BacktrackIntoHallway(RoomTag hallway)
    {
        if (hallway == null || hallway != currentHallway) return;

        // Mark reroll and clear everything in front of this hallway
        pendingReroll = true;
        DestroyFront();

        // Either rebuild right away (still while you're in the hallway),
        // or defer until you walk to the hallway end gate again.
        if (autoRebuildOnBacktrack)
        {
            BuildFrontIfNeeded();
        }
    }

}
