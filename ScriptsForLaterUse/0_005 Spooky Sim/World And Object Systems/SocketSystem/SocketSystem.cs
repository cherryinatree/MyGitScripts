using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcGen.Sockets
{
    /// <summary>
    /// Public-facing API for other systems to drive generation/timing however they want.
    /// You provide a layout strategy via IRoomLayoutStrategy and call RunStrategy(...).
    ///
    /// Performance note:
    /// DestroyRoom() can now hide a room immediately, then destroy its child objects
    /// over multiple frames to reduce destruction spikes.
    /// </summary>
    public class SocketSystem : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private SocketConnector connector;

        [Header("Parenting")]
        [Tooltip("Spawned rooms are parented here. Defaults to this.transform.")]
        public Transform roomsParent;

        [Header("Randomness")]
        [Tooltip("Optional fixed seed for deterministic runs. Leave 0 for random.")]
        public int seed = 0;

        [Header("Slow Destroy")]
        [Tooltip("If true, DestroyRoom hides the room immediately, then destroys its child objects over multiple frames.")]
        [SerializeField] private bool destroyChildrenOverTime = true;

        [Tooltip("How many child GameObjects from a room hierarchy should be destroyed per frame.")]
        [SerializeField, Min(1)] private int childObjectsDestroyedPerFrame = 4;

        [Tooltip("If true, the room disappears immediately before its children are slowly destroyed.")]
        [SerializeField] private bool hideRoomBeforeSlowDestroy = true;

        [Tooltip("If true, the room is detached from roomsParent before slow destruction.")]
        [SerializeField] private bool detachRoomBeforeSlowDestroy = true;

        [Tooltip("Optional parent for rooms waiting to be slowly destroyed. If empty, one is created automatically.")]
        [SerializeField] private Transform destroyQueueParent;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = false;

        public System.Random Rng { get; private set; }

        public IReadOnlyList<RoomSockets> ActiveRooms => _activeRooms;

        private readonly List<RoomSockets> _activeRooms = new();

        private readonly Queue<RoomSockets> _slowDestroyQueue = new();
        private readonly HashSet<int> _queuedForSlowDestroyIds = new();

        private Coroutine _slowDestroyRoutine;

        public event Action<RoomSockets> OnRoomSpawned;
        public event Action<RoomSockets> OnRoomDestroyed;

        private void Awake()
        {
            if (!connector)
                connector = GetComponent<SocketConnector>();

            if (!roomsParent)
                roomsParent = transform;

            if (!destroyQueueParent)
            {
                GameObject queueRoot = new GameObject($"{name}_DestroyQueue");
                queueRoot.transform.SetParent(transform, false);
                destroyQueueParent = queueRoot.transform;
            }

            Rng = seed == 0
                ? new System.Random()
                : new System.Random(seed);
        }

        // =========================================================
        // ======================= PUBLIC API ======================
        // =========================================================

        /// <summary>
        /// Spawn a room prefab at position/rotation. Sockets are auto-refreshed.
        /// </summary>
        public RoomSockets SpawnRoom(RoomSockets prefab, Vector3 position, Quaternion rotation)
        {
            if (!prefab)
                return null;

            RoomSockets inst = Instantiate(prefab, position, rotation, roomsParent);

            inst.RefreshSockets();

            _activeRooms.Add(inst);

            OnRoomSpawned?.Invoke(inst);

            if (verboseLogs)
                Debug.Log($"[SocketSystem] Spawned room '{inst.name}'.");

            return inst;
        }

        /// <summary>
        /// Destroy a room that was previously spawned by this system.
        /// If slow destroy is enabled, the room is hidden immediately and dismantled over multiple frames.
        /// </summary>
        public void DestroyRoom(RoomSockets room)
        {
            if (!room)
                return;

            _activeRooms.Remove(room);

            OnRoomDestroyed?.Invoke(room);

            if (destroyChildrenOverTime)
            {
                QueueRoomForSlowDestroy(room);
            }
            else
            {
                Destroy(room.gameObject);
            }
        }

        /// <summary>
        /// Instantiate a room prefab and attach it to a target socket by choosing
        /// a compatible candidate socket within the new room.
        /// </summary>
        public bool TryAttachRoomToSocket(
            RoomSockets roomPrefab,
            Socket targetSocket,
            Predicate<Socket> chooseCandidateSocket,
            out RoomSockets placedRoom)
        {
            placedRoom = null;

            if (!roomPrefab || !targetSocket)
                return false;

            // Instantiate candidate room in temp position near target.
            // The connector will move/rotate it into place.
            RoomSockets tempRoom = SpawnRoom(
                roomPrefab,
                targetSocket.transform.position,
                Quaternion.identity
            );

            if (!tempRoom)
                return false;

            Socket candidate = FindCandidateSocket(tempRoom, targetSocket, chooseCandidateSocket);

            if (!candidate)
            {
                DestroyRoom(tempRoom);
                return false;
            }

            if (!connector)
            {
                Debug.LogWarning("[SocketSystem] Missing SocketConnector.");
                DestroyRoom(tempRoom);
                return false;
            }

            if (!connector.TryConnect(targetSocket, candidate, out var rot, out var delta, out var bounds))
            {
                DestroyRoom(tempRoom);
                return false;
            }

            placedRoom = tempRoom;

            return true;
        }

        /// <summary>
        /// Return all currently free sockets across all active rooms.
        /// </summary>
        public IEnumerable<Socket> AllFreeSockets(Predicate<Socket> filter = null)
        {
            for (int i = 0; i < _activeRooms.Count; i++)
            {
                RoomSockets room = _activeRooms[i];

                if (!room)
                    continue;

                foreach (Socket socket in room.FreeSockets(filter))
                {
                    yield return socket;
                }
            }
        }

        /// <summary>
        /// Clear and destroy all spawned rooms.
        /// Uses slow destruction if destroyChildrenOverTime is enabled.
        /// </summary>
        public void ClearAll()
        {
            RoomSockets[] copy = _activeRooms.ToArray();

            for (int i = 0; i < copy.Length; i++)
            {
                DestroyRoom(copy[i]);
            }

            _activeRooms.Clear();
        }

        // =========================================================
        // ===================== SLOW DESTROY ======================
        // =========================================================

        private void QueueRoomForSlowDestroy(RoomSockets room)
        {
            if (!room)
                return;

            int instanceId = room.GetInstanceID();

            if (_queuedForSlowDestroyIds.Contains(instanceId))
                return;

            _queuedForSlowDestroyIds.Add(instanceId);

            if (hideRoomBeforeSlowDestroy)
                room.gameObject.SetActive(false);

            if (detachRoomBeforeSlowDestroy)
            {
                Transform targetParent = destroyQueueParent ? destroyQueueParent : null;
                room.transform.SetParent(targetParent, true);
            }

            _slowDestroyQueue.Enqueue(room);

            if (_slowDestroyRoutine == null)
                _slowDestroyRoutine = StartCoroutine(ProcessSlowDestroyQueue());

            if (verboseLogs)
                Debug.Log($"[SocketSystem] Queued room '{room.name}' for slow destroy.");
        }

        private IEnumerator ProcessSlowDestroyQueue()
        {
            while (_slowDestroyQueue.Count > 0)
            {
                RoomSockets room = _slowDestroyQueue.Dequeue();

                if (!room)
                    continue;

                int instanceId = room.GetInstanceID();

                yield return DestroyRoomHierarchyOverTime(room);

                _queuedForSlowDestroyIds.Remove(instanceId);
            }

            _slowDestroyRoutine = null;
        }

        private IEnumerator DestroyRoomHierarchyOverTime(RoomSockets room)
        {
            if (!room)
                yield break;

            List<GameObject> objectsToDestroy = new();

            CollectChildrenDeepestFirst(room.transform, objectsToDestroy);

            int destroyedThisFrame = 0;

            for (int i = 0; i < objectsToDestroy.Count; i++)
            {
                GameObject obj = objectsToDestroy[i];

                if (obj)
                    Destroy(obj);

                destroyedThisFrame++;

                if (destroyedThisFrame >= childObjectsDestroyedPerFrame)
                {
                    destroyedThisFrame = 0;
                    yield return null;
                }
            }

            // Give Unity a frame after child teardown before removing the root.
            yield return null;

            if (room)
                Destroy(room.gameObject);
        }

        private void CollectChildrenDeepestFirst(Transform root, List<GameObject> results)
        {
            if (!root)
                return;

            // Deepest first means grandchildren are queued before parents.
            // That prevents destroying one parent from wiping a huge branch in one frame.
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);

                CollectChildrenDeepestFirst(child, results);

                results.Add(child.gameObject);
            }
        }

        // =========================================================
        // ===================== SOCKET HELPERS ====================
        // =========================================================

        private Socket FindCandidateSocket(
            RoomSockets tempRoom,
            Socket targetSocket,
            Predicate<Socket> chooseCandidateSocket)
        {
            if (!tempRoom || !targetSocket)
                return null;

            tempRoom.RefreshSockets();

            foreach (Socket socket in tempRoom.FreeSockets())
            {
                if (!socket)
                    continue;

                if (!socket.IsCompatibleWith(targetSocket))
                    continue;

                if (chooseCandidateSocket != null && !chooseCandidateSocket(socket))
                    continue;

                return socket;
            }

            return null;
        }

        // =========================================================
        // ===================== STRATEGY RUNNER ===================
        // =========================================================

        /// <summary>
        /// Run a generation strategy as a coroutine.
        /// </summary>
        public Coroutine RunStrategy(IRoomLayoutStrategy strategy)
        {
            if (strategy == null)
                throw new ArgumentNullException(nameof(strategy));

            return StartCoroutine(ExecuteStrategy(strategy));
        }

        private IEnumerator ExecuteStrategy(IRoomLayoutStrategy strategy)
        {
            yield return strategy.Execute(this);
        }
    }

    /// <summary>
    /// Implement this in any class. It does not need to be a MonoBehaviour.
    /// Return an IEnumerator so you can time generation via yields.
    /// </summary>
    public interface IRoomLayoutStrategy
    {
        IEnumerator Execute(SocketSystem api);
    }
}