using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProcGen.Sockets;

namespace ProcGen.Anomalies
{
    /// <summary>
    /// Trigger API:
    ///   generator.HallwayForwardTriggered(hallwayRoot);
    ///   generator.HallwayBackTriggered(hallwayRoot);
    ///
    /// Gameplay meaning:
    ///   - Forward trigger = player accepts the current room as CLEAN.
    ///   - Back trigger = player rejects the current room as an ANOMALY.
    ///
    /// Generation behavior:
    ///   - When a hallway trigger fires:
    ///       1) Score the previous generated room, if one is armed.
    ///       2) Reset everything except the hallway that was triggered.
    ///       3) Spawn a new [room + hallway] in the chosen direction.
    ///
    /// First content rule:
    ///   - The first generated content room after the starting hallway is always clean.
    ///
    /// Performance:
    ///   - Optionally spreads reset/spawn work across frames to reduce frame spikes.
    ///   - This does not use true threading because Unity GameObjects must be created/destroyed
    ///     on the main thread.
    /// </summary>
    public class AnomalyRoomGenerator : MonoBehaviour
    {
        // -------- Inspector --------

        [Header("Socket System")]
        [SerializeField] private SocketSystem socketSystem;

        [Header("Prefabs: Base")]
        [SerializeField] private RoomSockets startRoomPrefab;
        [SerializeField] private RoomSockets hallwayPrefab;

        [Header("Prefabs: Content Pool")]
        [Tooltip("Anomaly rooms. These should have RoomTag.kind set to RoomKind.Anomaly.")]
        [SerializeField] private List<RoomSockets> anomalyRoomPrefabs = new();

        [Tooltip("Single clean room. This should NOT have RoomTag.kind set to RoomKind.Anomaly.")]
        [SerializeField] private RoomSockets cleanRoomPrefab;

        [Tooltip("End room. No hallway extension is spawned after this room.")]
        [SerializeField] private RoomSockets endRoomPrefab;

        [Header("Spawn Probabilities")]
        [Range(0f, 1f)]
        [SerializeField] private float cleanProbability = 0.20f;

        [Header("First Room Rule")]
        [Tooltip("If true, the first generated content room after the starting hallway will always be clean.")]
        [SerializeField] private bool firstGeneratedContentRoomIsClean = true;

        [Header("First Anomaly Rule")]
        [Tooltip("If true, the first anomaly room generated will use the selected prefab below.")]
        [SerializeField] private bool useGuaranteedFirstAnomalyRoom = true;

        [Tooltip("This anomaly prefab will be used for the first anomaly the player comes across.")]
        [SerializeField] private RoomSockets guaranteedFirstAnomalyRoomPrefab;

        [Tooltip("If true, the guaranteed anomaly appears as soon as the first-clean-room rule allows it. If false, it appears the first time RNG chooses an anomaly.")]
        [SerializeField] private bool forceGuaranteedFirstAnomalyAsSoonAsAllowed = false;


        [Header("End Logic")]
        [Tooltip("When CorrectStreak >= this number, the NEXT content room becomes the End Room.")]
        [SerializeField] public int streakToRevealEnd = 3;

        [Tooltip("Prevent further generation after End Room is placed.")]
        [SerializeField] private bool lockAfterEnd = true;

        [Header("Init")]
        [SerializeField] private Vector3 startPosition = Vector3.zero;
        [SerializeField] private Vector3 startForward = Vector3.forward;

        [Header("Performance")]
        [Tooltip("If true, reset/generation is spread across frames to reduce frame spikes.")]
        [SerializeField] private bool spreadGenerationAcrossFrames = true;

        [Tooltip("How many room/hallway destroy operations to attempt per frame during reset.")]
        [SerializeField, Min(1)] private int destroyedObjectsPerFrame = 2;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = false;

        // -------- Runtime state --------

        private RoomSockets _startRoom;

        private readonly Dictionary<RoomSockets, HallwayContext> _hallways = new();

        public int CorrectStreak { get; private set; }
        public event Action<int> OnStreakChanged;

        private bool _endPlaced = false;
        private bool _armed = false;
        private bool _generationInProgress = false;

        private int _generatedContentRoomCount = 0;

        private RoomSockets _currentRoom;

        private bool _hasPlacedAnyAnomalyRoom = false;
        private bool _currentRoomIsEndRoom = false;

        public RoomSockets CurrentRoom => _currentRoom;
        public bool CurrentRoomIsAnomaly => IsAnomaly(_currentRoom);
        public bool CurrentRoomIsEndRoom => _currentRoomIsEndRoom;
        public int GeneratedContentRoomCount => _generatedContentRoomCount;
        public bool GenerationInProgress => _generationInProgress;
        public bool EndPlaced => _endPlaced;

        public event Action<CurrentRoomInfo> OnCurrentRoomInfoChanged;

        [Serializable]
        public struct CurrentRoomInfo
        {
            public RoomSockets room;
            public string roomName;
            public string roomKind;
            public bool isAnomaly;
            public bool isEndRoom;
            public bool generationInProgress;
            public int generatedContentRoomCount;
            public int correctStreak;

            public bool HasRoom => room != null;

            public CurrentRoomInfo(
                RoomSockets room,
                string roomName,
                string roomKind,
                bool isAnomaly,
                bool isEndRoom,
                bool generationInProgress,
                int generatedContentRoomCount,
                int correctStreak)
            {
                this.room = room;
                this.roomName = roomName;
                this.roomKind = roomKind;
                this.isAnomaly = isAnomaly;
                this.isEndRoom = isEndRoom;
                this.generationInProgress = generationInProgress;
                this.generatedContentRoomCount = generatedContentRoomCount;
                this.correctStreak = correctStreak;
            }
        }

        public CurrentRoomInfo GetCurrentRoomInfo()
        {
            bool hasRoom = _currentRoom != null;
            bool isAnomaly = IsAnomaly(_currentRoom);

            string kind = "None";

            if (hasRoom)
            {
                if (_currentRoomIsEndRoom)
                    kind = "End Room";
                else if (isAnomaly)
                    kind = "Anomaly";
                else
                    kind = "Clean";
            }

            return new CurrentRoomInfo(
                _currentRoom,
                DisplayName(_currentRoom),
                kind,
                isAnomaly,
                _currentRoomIsEndRoom,
                _generationInProgress,
                _generatedContentRoomCount,
                CorrectStreak
            );
        }

        private void NotifyCurrentRoomInfoChanged()
        {
            OnCurrentRoomInfoChanged?.Invoke(GetCurrentRoomInfo());
        }

        // =========================================================
        // ===================== UNITY LIFECYCLE ===================
        // =========================================================

        private void Awake()
        {
            if (!socketSystem)
                socketSystem = GetComponent<SocketSystem>();
        }
        private void Start()
        {
            PlaceStartAndFirstHallway();
            NotifyCurrentRoomInfoChanged();
        }
        // =========================================================
        // ================= ENTRY POINTS / TRIGGERS ===============
        // =========================================================

        /// <summary>
        /// Call this from the FORWARD trigger on a hallway.
        /// This means the player believes the current room was clean.
        /// </summary>
        public void HallwayForwardTriggered(RoomSockets hallway)
        {
            if (!ValidateTriggerInput(hallway)) return;
            if (_generationInProgress) return;
            if (lockAfterEnd && _endPlaced) return;

            var ctx = EnsureContext(hallway);

            if (_armed)
            {
                ForwardGuess(ctx);
                _armed = false;
            }

            RequestSpawn(hallway, forward: true);
        }


        public void HallwayForwardTriggeredBackGuess(RoomSockets hallway)
        {
            if (!ValidateTriggerInput(hallway)) return;
            if (_generationInProgress) return;
            if (lockAfterEnd && _endPlaced) return;

            var ctx = EnsureContext(hallway);

            if (_armed)
            {
                BackGuess(ctx);
                _armed = false;
            }

            RequestSpawn(hallway, forward: true);
        }
        /// <summary>
        /// Call this from the BACK trigger on a hallway.
        /// This means the player believes the current room was an anomaly.
        /// </summary>
        public void HallwayBackTriggered(RoomSockets hallway)
        {
            if (!ValidateTriggerInput(hallway)) return;
            if (_generationInProgress) return;
            if (lockAfterEnd && _endPlaced) return;

            var ctx = EnsureContext(hallway);

            if (_armed)
            {
                BackGuess(ctx);
                _armed = false;
            }

            RequestSpawn(hallway, forward: false);
        }

        /// <summary>
        /// Compatibility method if you still have old scene hooks.
        /// Prefer HallwayForwardTriggered instead.
        /// </summary>
        public void SpawnForward(RoomSockets hallway)
        {
            RequestSpawn(hallway, forward: true);
        }

        /// <summary>
        /// Compatibility method if you still have old scene hooks.
        /// Prefer HallwayBackTriggered instead.
        /// </summary>
        public void SpawnBackward(RoomSockets hallway)
        {
            RequestSpawn(hallway, forward: false);
        }

        public void OnRoomEntered(RoomSockets room)
        {
            if (verboseLogs)
                Log($"OnRoomEntered ignored for '{Name(room)}'.");
        }

        // =========================================================
        // ===================== SPAWN REQUEST =====================
        // =========================================================

        private void RequestSpawn(RoomSockets hallway, bool forward)
        {
            if (!ValidateTriggerInput(hallway)) return;
            if (_generationInProgress) return;
            if (lockAfterEnd && _endPlaced) return;

            if (spreadGenerationAcrossFrames)
            {
                StartCoroutine(SpawnRoutine(hallway, forward));
            }
            else
            {
                SpawnImmediate(hallway, forward);
            }
        }

        private void SpawnImmediate(RoomSockets hallway, bool forward)
        {
            var ctx = EnsureContext(hallway);

            ResetToOnlyThisHallway(ctx);

            _armed = SpawnChainInDirection(ctx, forward);
        }
        private IEnumerator SpawnRoutine(RoomSockets hallway, bool forward)
        {
            _generationInProgress = true;
            NotifyCurrentRoomInfoChanged();

            var ctx = EnsureContext(hallway);

            yield return ResetToOnlyThisHallwayRoutine(ctx);

            // Give Unity a frame to breathe before attaching new prefabs.
            yield return null;

            _armed = SpawnChainInDirection(ctx, forward);

            _generationInProgress = false;
            NotifyCurrentRoomInfoChanged();
        }

        // =========================================================
        // ======================= RESET CORE ======================
        // =========================================================

        /// <summary>
        /// Destroy all rooms and hallways except the hallway that was just triggered.
        /// </summary>
        private void ResetToOnlyThisHallway(HallwayContext keepCtx)
        {
            if (verboseLogs)
                Log($"RESET: Keeping only hallway '{Name(keepCtx.hallway)}'.");

            TryDestroyRoom(keepCtx.frontRoom);
            keepCtx.frontRoom = null;

            TryDestroyRoom(keepCtx.backRoom);
            keepCtx.backRoom = null;

            SafeFreeSocket(keepCtx.forwardSocket);
            SafeFreeSocket(keepCtx.backSocket);

            keepCtx.forwardGenerated = false;

            var keys = new List<RoomSockets>(_hallways.Keys);

            foreach (var h in keys)
            {
                if (h == keepCtx.hallway)
                    continue;

                if (_hallways.TryGetValue(h, out var other))
                {
                    TryDestroyRoom(other.frontRoom);
                    TryDestroyRoom(other.backRoom);
                }

                TryDestroyRoom(h);
                _hallways.Remove(h);
            }
        }

        /// <summary>
        /// Coroutine version of reset.
        /// This spreads destruction work across frames to reduce stutter.
        /// </summary>
        private IEnumerator ResetToOnlyThisHallwayRoutine(HallwayContext keepCtx)
        {
            if (verboseLogs)
                Log($"RESET ROUTINE: Keeping only hallway '{Name(keepCtx.hallway)}'.");

            int destroyedThisFrame = 0;

            TryDestroyRoom(keepCtx.frontRoom);
            keepCtx.frontRoom = null;
            destroyedThisFrame++;

            if (destroyedThisFrame >= destroyedObjectsPerFrame)
            {
                destroyedThisFrame = 0;
                yield return null;
            }

            TryDestroyRoom(keepCtx.backRoom);
            keepCtx.backRoom = null;
            destroyedThisFrame++;

            if (destroyedThisFrame >= destroyedObjectsPerFrame)
            {
                destroyedThisFrame = 0;
                yield return null;
            }

            SafeFreeSocket(keepCtx.forwardSocket);
            SafeFreeSocket(keepCtx.backSocket);

            keepCtx.forwardGenerated = false;

            var keys = new List<RoomSockets>(_hallways.Keys);

            foreach (var h in keys)
            {
                if (h == keepCtx.hallway)
                    continue;

                if (_hallways.TryGetValue(h, out var other))
                {
                    TryDestroyRoom(other.frontRoom);
                    destroyedThisFrame++;

                    if (destroyedThisFrame >= destroyedObjectsPerFrame)
                    {
                        destroyedThisFrame = 0;
                        yield return null;
                    }

                    TryDestroyRoom(other.backRoom);
                    destroyedThisFrame++;

                    if (destroyedThisFrame >= destroyedObjectsPerFrame)
                    {
                        destroyedThisFrame = 0;
                        yield return null;
                    }
                }

                TryDestroyRoom(h);
                _hallways.Remove(h);
                destroyedThisFrame++;

                if (destroyedThisFrame >= destroyedObjectsPerFrame)
                {
                    destroyedThisFrame = 0;
                    yield return null;
                }
            }
        }

        // =========================================================
        // ================= SPAWN IN A DIRECTION ==================
        // =========================================================

        /// <summary>
        /// Spawns [content room + hallway] in the given direction from the current hallway.
        /// Returns true if a normal content room was placed and should be scored next time.
        /// Returns false if spawning failed or the End Room was placed.
        /// </summary>
        private bool SpawnChainInDirection(HallwayContext ctx, bool forward)
        {
            if (ctx == null || !ctx.hallway)
            {
                LogWarn("SpawnChainInDirection failed because context or hallway was null.");
                return false;
            }

            Vector3 dir = forward
                ? ctx.hallway.transform.forward
                : -ctx.hallway.transform.forward;

            Socket socket = forward
                ? ctx.forwardSocket ?? FindForwardSocket(ctx.hallway)
                : ctx.backSocket ?? FindBackSocket(ctx.hallway);

            if (!socket)
            {
                LogWarn($"No {(forward ? "forward" : "back")} socket on {Name(ctx.hallway)}.");
                return false;
            }

            var (contentPrefab, isEnd, expectedAnomaly) = ChooseNextContent();

            if (!contentPrefab)
            {
                LogWarn("ChooseNextContent returned no prefab.");
                return false;
            }

            if (!AttachContentAtSocket(socket, contentPrefab, out var placedRoom))
            {
                LogWarn($"Failed to place {(forward ? "FORWARD" : "BACK")} content for {Name(ctx.hallway)}.");
                return false;
            }

            if (forward)
            {
                ctx.frontRoom = placedRoom;
                ctx.forwardGenerated = true;
            }
            else
            {
                ctx.backRoom = placedRoom;
            }

            _currentRoom = placedRoom;
            _currentRoomIsEndRoom = isEnd;
            _generatedContentRoomCount++;

            bool actualAnomaly = IsAnomaly(placedRoom);

            if (actualAnomaly)
                _hasPlacedAnyAnomalyRoom = true;

            NotifyCurrentRoomInfoChanged();

            if (_generatedContentRoomCount == 1 && firstGeneratedContentRoomIsClean && actualAnomaly)
            {
                LogWarn("The first generated room was supposed to be clean, but the placed room is tagged as an anomaly.");
            }

            if (expectedAnomaly && !actualAnomaly)
            {
                LogWarn($"'{placedRoom.name}' was selected as an anomaly prefab, but it is not tagged as RoomKind.Anomaly.");
            }
            if (isEnd)
            {
                _endPlaced = true;

                if (verboseLogs)
                    Log($"End Room placed {(forward ? "FORWARD" : "BACK")} of {Name(ctx.hallway)}.");

                return false;
            }

            ExtendWithHallwayFromRoom(
                fromRoom: placedRoom,
                extendDirection: dir,
                out var newHall
            );

            if (!newHall)
            {
                LogWarn($"Content room '{placedRoom.name}' was placed, but no new hallway could be extended from it.");
                return true;
            }

            if (verboseLogs)
            {
                Log($"{(forward ? "FORWARD" : "BACK")} generated on {Name(ctx.hallway)} -> '{placedRoom.name}' plus hallway '{Name(newHall)}'.");
            }

            return true;
        }

        // =========================================================
        // ======================== SCORING ========================
        // =========================================================

        /// <summary>
        /// Player moved forward, so they guessed the current room was clean.
        /// Correct if current room is NOT an anomaly.
        /// </summary>
        private void ForwardGuess(HallwayContext ctx)
        {
            if (!_currentRoom)
            {
                LogWarn("ForwardGuess called, but there is no current room to score.");
                return;
            }

            if (!IsAnomaly(_currentRoom))
            {
                CorrectStreak++;

                if (verboseLogs)
                    Log($"Correct clean guess. Streak = {CorrectStreak}");
            }
            else
            {
                CorrectStreak = 0;

                if (verboseLogs)
                    Log($"Wrong clean guess. '{_currentRoom.name}' was an anomaly. Streak reset.");
            }

            OnStreakChanged?.Invoke(CorrectStreak); 
            NotifyCurrentRoomInfoChanged();
        }

        /// <summary>
        /// Player moved backward, so they guessed the current room was an anomaly.
        /// Correct if current room IS an anomaly.
        /// </summary>
        private void BackGuess(HallwayContext ctx)
        {
            if (!_currentRoom)
            {
                LogWarn("BackGuess called, but there is no current room to score.");
                return;
            }

            if (IsAnomaly(_currentRoom))
            {
                CorrectStreak++;

                if (verboseLogs)
                    Log($"Correct anomaly guess. Streak = {CorrectStreak}");
            }
            else
            {
                CorrectStreak = 0;

                if (verboseLogs)
                    Log($"Wrong anomaly guess. '{_currentRoom.name}' was clean. Streak reset.");
            }

            OnStreakChanged?.Invoke(CorrectStreak);
        }

        // =========================================================
        // =================== STARTUP HELPERS =====================
        // =========================================================

        private void PlaceStartAndFirstHallway()
        {
            if (!socketSystem)
            {
                LogWarn("Missing SocketSystem.");
                return;
            }

            if (!startRoomPrefab)
            {
                LogWarn("Missing Start Room Prefab.");
                return;
            }

            if (!hallwayPrefab)
            {
                LogWarn("Missing Hallway Prefab.");
                return;
            }

            Vector3 safeForward = startForward == Vector3.zero
                ? Vector3.forward
                : startForward.normalized;

            _startRoom = socketSystem.SpawnRoom(
                startRoomPrefab,
                startPosition,
                Quaternion.LookRotation(safeForward, Vector3.up)
            );

            if (!_startRoom)
            {
                LogWarn("Failed to spawn StartRoom.");
                return;
            }

            Socket startSocket = ChooseSocketByPosition(
                _startRoom,
                _startRoom.transform.forward,
                mustBeFree: false
            );

            if (!startSocket)
            {
                LogWarn("StartRoom has no suitable forward-positioned socket.");
                return;
            }

            if (!socketSystem.TryAttachRoomToSocket(hallwayPrefab, startSocket, s => true, out var firstHall))
            {
                LogWarn("Failed to attach initial Hallway to StartRoom.");
                return;
            }

            var hctx = CreateHallwayContext(firstHall);

            // This is informational. When generation starts, reset removes all rooms except the current hallway.
            hctx.backRoom = _startRoom;

            if (verboseLogs)
                Log($"Init OK. Start='{_startRoom.name}', FirstHall='{firstHall.name}'");
        }

        // =========================================================
        // =================== ATTACH / EXTEND =====================
        // =========================================================

        private bool AttachContentAtSocket(Socket targetSocket, RoomSockets contentPrefab, out RoomSockets placedRoom)
        {
            placedRoom = null;

            if (!socketSystem)
            {
                LogWarn("Cannot attach content because SocketSystem is missing.");
                return false;
            }

            if (!targetSocket)
            {
                LogWarn("Cannot attach content because target socket is missing.");
                return false;
            }

            if (!contentPrefab)
            {
                LogWarn("Cannot attach content because content prefab is missing.");
                return false;
            }

            return socketSystem.TryAttachRoomToSocket(contentPrefab, targetSocket, s => true, out placedRoom);
        }

        private void ExtendWithHallwayFromRoom(RoomSockets fromRoom, Vector3 extendDirection, out RoomSockets newHallway)
        {
            newHallway = null;

            if (!fromRoom)
            {
                LogWarn("Cannot extend hallway from a null room.");
                return;
            }

            if (!hallwayPrefab)
            {
                LogWarn("Cannot extend hallway because hallwayPrefab is missing.");
                return;
            }

            Socket exitSocket =
                ChooseSocketByPosition(fromRoom, extendDirection, mustBeFree: true) ??
                fromRoom.FreeSockets().FirstOrDefault();

            if (exitSocket == null)
            {
                if (verboseLogs)
                    LogWarn($"No exit socket found on '{fromRoom.name}' to extend hallway.");

                return;
            }

            if (!socketSystem.TryAttachRoomToSocket(hallwayPrefab, exitSocket, s => true, out var hall))
            {
                if (verboseLogs)
                    LogWarn($"Failed to attach hallway to exit socket of '{fromRoom.name}'.");

                return;
            }

            newHallway = hall;

            CreateHallwayContext(hall);

            if (verboseLogs)
                Log($"Extended from '{fromRoom.name}' with hallway '{hall.name}'.");
        }

        // =========================================================
        // ======================== CONTEXT ========================
        // =========================================================

        private HallwayContext EnsureContext(RoomSockets hallway)
        {
            if (!_hallways.TryGetValue(hallway, out var ctx))
            {
                ctx = CreateHallwayContext(hallway);
            }
            else
            {
                if (!ctx.forwardSocket)
                    ctx.forwardSocket = FindForwardSocket(hallway);

                if (!ctx.backSocket)
                    ctx.backSocket = FindBackSocket(hallway);
            }

            return ctx;
        }

        private HallwayContext CreateHallwayContext(RoomSockets hallway)
        {
            var ctx = new HallwayContext
            {
                hallway = hallway,
                forwardSocket = FindForwardSocket(hallway),
                backSocket = FindBackSocket(hallway)
            };

            _hallways[hallway] = ctx;

            return ctx;
        }

        // =========================================================
        // ===================== SOCKET PICKERS ====================
        // =========================================================

        private Socket FindForwardSocket(RoomSockets hallway)
        {
            return ChooseSocketByPosition(hallway, hallway.transform.forward, mustBeFree: false);
        }

        private Socket FindBackSocket(RoomSockets hallway)
        {
            return ChooseSocketByPosition(hallway, -hallway.transform.forward, mustBeFree: false);
        }

        /// <summary>
        /// Pick the socket whose position lies most along worldDir from the room origin.
        /// This is usually more stable than relying on each socket's own forward direction.
        /// </summary>
        private Socket ChooseSocketByPosition(RoomSockets room, Vector3 worldDir, bool mustBeFree)
        {
            if (!room || room.Sockets == null)
                return null;

            worldDir = worldDir == Vector3.zero
                ? room.transform.forward
                : worldDir.normalized;

            Vector3 origin = room.transform.position;

            Socket best = null;
            float bestDot = float.NegativeInfinity;

            foreach (var socket in room.Sockets)
            {
                if (!socket)
                    continue;

                if (mustBeFree && socket.Occupied)
                    continue;

                Vector3 dirFromCenter = socket.transform.position - origin;

                if (dirFromCenter.sqrMagnitude < 0.000001f)
                    continue;

                float dot = Vector3.Dot(dirFromCenter.normalized, worldDir);

                if (dot > bestDot)
                {
                    bestDot = dot;
                    best = socket;
                }
            }

            return best;
        }

        // =========================================================
        // ===================== SELECTION / RNG ===================
        // =========================================================

        private (RoomSockets prefab, bool isEnd, bool isAnomaly) ChooseNextContent()
        {
            // First generated content room must be clean.
            if (firstGeneratedContentRoomIsClean && _generatedContentRoomCount == 0)
            {
                if (cleanRoomPrefab)
                    return (cleanRoomPrefab, false, false);

                LogWarn("First generated room should be clean, but cleanRoomPrefab is missing.");
            }

            // Optional: force the chosen anomaly as soon as the first-clean rule allows it.
            if (forceGuaranteedFirstAnomalyAsSoonAsAllowed && CanUseGuaranteedFirstAnomalyNow())
            {
                return (guaranteedFirstAnomalyRoomPrefab, false, true);
            }

            // End gate.
            // This runs after the first-room-clean rule so your very first room cannot become the End Room.
            if (!_endPlaced && streakToRevealEnd > 0 && CorrectStreak >= streakToRevealEnd && endRoomPrefab)
            {
                return (endRoomPrefab, true, false);
            }

            double roll = socketSystem?.Rng?.NextDouble() ?? UnityEngine.Random.value;

            bool chooseClean = roll < cleanProbability && cleanRoomPrefab;

            if (chooseClean)
                return (cleanRoomPrefab, false, false);

            RoomSockets anomaly = PickAnomalyPrefab();

            if (!anomaly && cleanRoomPrefab)
                return (cleanRoomPrefab, false, false);

            return (anomaly, false, true);
        }

        private RoomSockets RandomAnomalyPrefab()
        {
            if (anomalyRoomPrefabs == null || anomalyRoomPrefabs.Count == 0)
                return null;

            int index = socketSystem?.Rng != null
                ? socketSystem.Rng.Next(0, anomalyRoomPrefabs.Count)
                : UnityEngine.Random.Range(0, anomalyRoomPrefabs.Count);

            return anomalyRoomPrefabs[index];
        }
        private bool CanUseGuaranteedFirstAnomalyNow()
        {
            if (!useGuaranteedFirstAnomalyRoom)
                return false;

            if (_hasPlacedAnyAnomalyRoom)
                return false;

            if (!guaranteedFirstAnomalyRoomPrefab)
                return false;

            // Respect the existing first-room-clean rule.
            if (firstGeneratedContentRoomIsClean && _generatedContentRoomCount == 0)
                return false;

            return true;
        }

        private RoomSockets PickAnomalyPrefab()
        {
            if (CanUseGuaranteedFirstAnomalyNow())
                return guaranteedFirstAnomalyRoomPrefab;

            return RandomAnomalyPrefab();
        }

        private static string DisplayName(RoomSockets room)
        {
            if (!room)
                return "None";

            return room.name.Replace("(Clone)", "").Trim();
        }
        // =========================================================
        // ====================== UTIL / SAFETY ====================
        // =========================================================

        private bool ValidateTriggerInput(RoomSockets hallway)
        {
            if (hallway)
                return true;

            LogWarn("Trigger called with null hallway.");
            return false;
        }

        private void TryDestroyRoom(RoomSockets room)
        {
            if (!room)
                return;

            try
            {
                socketSystem.DestroyRoom(room);
            }
            catch (Exception exception)
            {
                LogWarn($"DestroyRoom exception on '{room.name}': {exception.Message}");

                // Fallback just in case the socket system fails to destroy it.
                Destroy(room.gameObject);
            }
        }

        private void SafeFreeSocket(Socket socket)
        {
            if (!socket)
                return;

            try
            {
                socket.MarkOccupied(false);
            }
            catch
            {
                // Some socket implementations may not support this safely after destruction.
            }
        }

        private static bool IsAnomaly(RoomSockets room)
        {
            RoomTag tag = room ? room.GetComponent<RoomTag>() : null;
            return tag && tag.kind == RoomKind.Anomaly;
        }

        private static string Name(RoomSockets room)
        {
            return room ? room.name : "<null>";
        }

        private void Log(string message)
        {
            Debug.Log($"[AnomalyGen] {message}");
        }

        private void LogWarn(string message)
        {
            Debug.LogWarning($"[AnomalyGen] {message}");
        }
    }

    // ----------------------------- Context -----------------------------

    internal class HallwayContext
    {
        public RoomSockets hallway;

        public Socket forwardSocket;
        public Socket backSocket;

        public RoomSockets frontRoom;
        public RoomSockets backRoom;

        public bool forwardGenerated;
    }
}