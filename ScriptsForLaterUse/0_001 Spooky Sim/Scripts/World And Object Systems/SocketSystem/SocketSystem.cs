using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProcGen.Sockets
{
    /// <summary>
    /// Public-facing API for other systems to drive generation/timing however they want.
    /// You provide a layout strategy (via IRoomLayoutStrategy), and call RunStrategy(...).
    /// Also provides helper methods like AttachRoomToSocket(...) and SpawnRoom(...).
    /// </summary>
    public class SocketSystem : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private SocketConnector connector;

        [Header("Parenting")]
        [Tooltip("Spawned rooms are parented here (defaults to this.transform).")]
        public Transform roomsParent;

        [Header("Randomness")]
        [Tooltip("Optional fixed seed for deterministic runs. Leave 0 for random.")]
        public int seed = 0;

        public System.Random Rng { get; private set; }
        public IReadOnlyList<RoomSockets> ActiveRooms => _activeRooms;

        private readonly List<RoomSockets> _activeRooms = new();

        public event Action<RoomSockets> OnRoomSpawned;
        public event Action<RoomSockets> OnRoomDestroyed;

        private void Awake()
        {
            if (!connector) connector = GetComponent<SocketConnector>();
            if (!roomsParent) roomsParent = transform;
            Rng = (seed == 0) ? new System.Random() : new System.Random(seed);
        }

        // ---------- Public API ----------

        /// <summary>Spawn a room prefab at position/rotation. Sockets are auto-refreshed.</summary>
        public RoomSockets SpawnRoom(RoomSockets prefab, Vector3 position, Quaternion rotation)
        {
            if (!prefab) return null;
            var inst = Instantiate(prefab, position, rotation, roomsParent);
            inst.RefreshSockets();
            _activeRooms.Add(inst);
            OnRoomSpawned?.Invoke(inst);
            return inst;
        }

        /// <summary>Destroy a room that was previously spawned by this system.</summary>
        public void DestroyRoom(RoomSockets room)
        {
            if (!room) return;
            _activeRooms.Remove(room);
            OnRoomDestroyed?.Invoke(room);
            Destroy(room.gameObject);
        }

        /// <summary>
        /// Instantiate a room prefab and attach it to a target socket by choosing a compatible candidate socket within the new room.
        /// </summary>
        public bool TryAttachRoomToSocket(RoomSockets roomPrefab, Socket targetSocket, Predicate<Socket> chooseCandidateSocket, out RoomSockets placedRoom)
        {
            placedRoom = null;
            if (!roomPrefab || !targetSocket) return false;

            // Instantiate candidate room in temp position near target (we’ll move/rotate it to fit)
            var tempRoom = SpawnRoom(roomPrefab, targetSocket.transform.position, Quaternion.identity);

            // Pick a candidate socket from the new room
            var candidate = tempRoom.FreeSockets(s => s.IsCompatibleWith(targetSocket) && (chooseCandidateSocket == null || chooseCandidateSocket(s))).FirstOrDefault();
            if (!candidate)
            {
                DestroyRoom(tempRoom);
                return false;
            }

            // Try to connect
            if (!connector.TryConnect(targetSocket, candidate, out var rot, out var delta, out var bounds))
            {
                DestroyRoom(tempRoom);
                return false;
            }

            placedRoom = tempRoom;
            return true;
        }

        /// <summary>Return all currently free sockets across all active rooms.</summary>
        public IEnumerable<Socket> AllFreeSockets(Predicate<Socket> filter = null)
        {
            foreach (var r in _activeRooms)
            {
                foreach (var s in r.FreeSockets(filter))
                    yield return s;
            }
        }

        /// <summary>Clear (destroy) all spawned rooms.</summary>
        public void ClearAll()
        {
            // copy first
            var copy = _activeRooms.ToArray();
            foreach (var r in copy) DestroyRoom(r);
            _activeRooms.Clear();
        }

        // ---------- Strategy Runner ----------

        /// <summary>
        /// Run a generation strategy as a coroutine (lets the strategy yield for timing).
        /// </summary>
        public Coroutine RunStrategy(IRoomLayoutStrategy strategy)
        {
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));
            return StartCoroutine(ExecuteStrategy(strategy));
        }

        private IEnumerator ExecuteStrategy(IRoomLayoutStrategy strategy)
        {
            yield return strategy.Execute(this);
        }
    }

    /// <summary>
    /// Implement this in ANY class (doesn't need to be a MonoBehaviour).
    /// Return an IEnumerator so you can time generation via yields (frames, WaitForSeconds, etc.)
    /// </summary>
    public interface IRoomLayoutStrategy
    {
        IEnumerator Execute(SocketSystem api);
    }
}
