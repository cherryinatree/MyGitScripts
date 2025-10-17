using System;
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
    /// Core behavior (your request):
    ///   - When either trigger fires:
    ///       1) Reset: clear ALL rooms & hallways except the CURRENT hallway.
    ///       2) Spawn: attach [room + hallway] in the direction of travel.
    ///
    /// Scoring (kept, but independent of reset):
    ///   - If Back fires AFTER Forward on the same hallway, we use a cached flag
    ///     (frontWasAnomalyAtArm) to evaluate the guess.
    ///
    /// Sockets are picked by POSITION (most along hallway’s world ±forward).
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
        [Tooltip("Anomaly rooms (have AnomalyTag.isAnomaly = true). One is chosen randomly when anomaly is selected.")]
        [SerializeField] private List<RoomSockets> anomalyRoomPrefabs = new();
        [Tooltip("Single clean room (no AnomalyTag).")]
        [SerializeField] private RoomSockets cleanRoomPrefab;
        [Tooltip("End room (no extension beyond it).")]
        [SerializeField] private RoomSockets endRoomPrefab;

        [Header("Spawn Probabilities")]
        [Range(0f, 1f)]
        [SerializeField] private float cleanProbability = 0.20f;

        [Header("End Logic")]
        [Tooltip("When streak >= threshold, the NEXT content becomes End Room.")]
        [SerializeField] private int streakToRevealEnd = 3;
        [Tooltip("Prevent further generation after End Room placed.")]
        [SerializeField] private bool lockAfterEnd = true;

        [Header("Init")]
        [SerializeField] private Vector3 startPosition = Vector3.zero;
        [SerializeField] private Vector3 startForward = Vector3.forward;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = false;

        // -------- Runtime state --------
        private RoomSockets _startRoom;

        // We track all hallways we’ve spawned and their local context
        private readonly Dictionary<RoomSockets, HallwayContext> _hallways = new();

        // Public scoring surface
        public int CorrectStreak { get; private set; }
        public event Action<int> OnStreakChanged;

        private bool _endPlaced = false;

        private bool armed = false;
        private bool isAnomaly = false;

        RoomSockets currentRoom;

        // ===== Unity lifecycle =====
        private void Awake()
        {
            if (!socketSystem) socketSystem = GetComponent<SocketSystem>();
        }

        private void Start()
        {
            PlaceStartAndFirstHallway();
        }

        // =========================================================
        // ===============  ENTRY POINTS / TRIGGERS  ===============
        // =========================================================

        /// <summary>Forward trigger on a hallway.</summary>
        public void HallwayForwardTriggered(RoomSockets hallway)
        {
            if (!ValidateTriggerInput(hallway)) return;
            if (lockAfterEnd && _endPlaced) return;

            var ctx = EnsureContext(hallway);
            /*
                        if (ctx.armedFromForward)
                        {

                            ForwardGuess(ctx);
                        }
                        else if (ctx.armedForForwardGuess)
                        {

                            ForwardGuess(ctx);
                        }
            */

            if (armed)
            {
                ForwardGuess(ctx);
            }
            // 3) Arm for scoring (if the player later goes Back)
           //ArmForFrontRoom(ctx);

        }

        /// <summary>Back trigger on a hallway.</summary>
        public void HallwayBackTriggered(RoomSockets hallway)
        {
            if (!ValidateTriggerInput(hallway)) return;
            if (lockAfterEnd && _endPlaced) return;

            var ctx = EnsureContext(hallway);


            /*
            if (ctx.armedFromForward)
            {

                // Score BEFORE we rebuild (scoring uses cached flag, not the live room)
                BackGuess(ctx);
            }
            else if(ctx.armedForForwardGuess)
            {

                BackGuess(ctx);
            }*/


            if (armed)
            {
                BackGuess(ctx);
            }

            // ArmedForBackRoom(ctx);
        }


        public void SpawnForward(RoomSockets hallway)
        {

            var ctx = EnsureContext(hallway);
            // 1) RESET: wipe everything except THIS hallway
            ResetToOnlyThisHallway(ctx);

            // 2) SPAWN: build forward chain [room + hallway]
            SpawnChainInDirection(ctx, forward: true);
            armed = true;
        }
        public void SpawnBackward(RoomSockets hallway)
        {

            var ctx = EnsureContext(hallway);

            // 1) RESET: wipe everything except THIS hallway
            ResetToOnlyThisHallway(ctx);

            // 2) SPAWN: build backward chain [room + hallway]
            SpawnChainInDirection(ctx, forward: false);
            armed = true;
        }


        // Left as a no-op (compatibility if you still have hooks in scene)
        public void OnRoomEntered(RoomSockets room)
        {
            if (verboseLogs) Log($"OnRoomEntered ignored for '{(room ? room.name : "<null>")}'.");
        }

        // =========================================================
        // =====================  RESET CORE  ======================
        // =========================================================

        /// <summary>
        /// Destroy ALL rooms and hallways except the current hallway. Also clears any rooms attached to this hallway.
        /// </summary>
        private void ResetToOnlyThisHallway(HallwayContext keepCtx)
        {
            if (verboseLogs) Log($"RESET: Keeping only hallway '{Name(keepCtx.hallway)}'.");

            // 1) Destroy rooms attached to the current hallway
            TryDestroyRoom(keepCtx.frontRoom); keepCtx.frontRoom = null;
            TryDestroyRoom(keepCtx.backRoom); keepCtx.backRoom = null;
            SafeFreeSocket(keepCtx.forwardSocket);
            SafeFreeSocket(keepCtx.backSocket);
            keepCtx.forwardGenerated = false;   // we will regenerate as needed
            // keepCtx.armedFromForward left as-is (we manage scoring on triggers)

            // 2) Destroy ALL other hallways (and their rooms), remove from registry
            var keys = new List<RoomSockets>(_hallways.Keys);
            foreach (var h in keys)
            {
                if (h == keepCtx.hallway) continue; // skip the hallway we keep

                if (_hallways.TryGetValue(h, out var other))
                {
                    // Destroy attached rooms first (be explicit; don’t rely on cascade)
                    TryDestroyRoom(other.frontRoom);
                    TryDestroyRoom(other.backRoom);
                }

                TryDestroyRoom(h);
                _hallways.Remove(h);
            }
        }

        // =========================================================
        // ===================  SPAWN IN A DIRECTION  ==============
        // =========================================================

        /// <summary>
        /// Spawns [content room + hallway] in the given direction from the current hallway.
        /// </summary>
        private void SpawnChainInDirection(HallwayContext ctx, bool forward)
        {
            // Pick socket by position along world ±forward
            var dir = forward ? ctx.hallway.transform.forward : -ctx.hallway.transform.forward;
            var socket = forward
                ? (ctx.forwardSocket ?? FindForwardSocket(ctx.hallway))
                : (ctx.backSocket ?? FindBackSocket(ctx.hallway));

            if (!socket)
            {
                LogWarn($"No {(forward ? "forward" : "back")} socket on {Name(ctx.hallway)}.");
                return;
            }

            // Decide what to place
            var (contentPrefab, isEnd, _) = ChooseNextContent();

            // Attach room to the chosen side
            if (!AttachContentAtSocket(socket, contentPrefab, out var placedRoom))
            {
                LogWarn($"Failed to place {(forward ? "FORWARD" : "BACK")} content for {Name(ctx.hallway)}.");
                return;
            }

            if (forward) { ctx.frontRoom = placedRoom; ctx.forwardGenerated = true; }
            else { ctx.backRoom = placedRoom; }

            currentRoom = placedRoom;


            if (isEnd)
            {
                _endPlaced = true;
                if (verboseLogs) Log($"🏁 End Room placed {(forward ? "FORWARD" : "BACK")} of {Name(ctx.hallway)}.");
                return; // no hallway extension after end
            }

            // Extend a hallway away from the content room (still the player’s direction)
            ExtendWithHallwayFromRoom(
                fromRoom: placedRoom,
                extendDirection: dir,
                ownerHallway: ctx.hallway,
                out var newHall
            );

            if (verboseLogs)
                Log($"{(forward ? "FORWARD" : "BACK")} generated on {Name(ctx.hallway)} → '{placedRoom.name}' (+ hallway='{Name(newHall)}').");
        }

        // =========================================================
        // ======================  SCORING  ========================
        // =========================================================

        /// <summary>After a FORWARD, arm the hallway so the next BACK scores against the current front room.</summary>
        private void ArmForFrontRoom(HallwayContext ctx)
        {
            ctx.armedFromForward = true;
            ctx.frontWasAnomalyAtArm = IsAnomaly(ctx.frontRoom); // cache for post-reset scoring
            //if (verboseLogs) Log($"Armed {Name(ctx.hallway)} for back-guess. frontIsAnomaly={ctx.frontWasAnomalyAtArm}");
        }

        /// <summary>After a FORWARD, arm the hallway so the next BACK scores against the current front room.</summary>
        private void ArmedForBackRoom(HallwayContext ctx)
        {
            ctx.armedForForwardGuess = true;
            ctx.frontWasAnomalyAtArm = IsAnomaly(ctx.backRoom); // cache for post-reset scoring
                                                                // if (verboseLogs) Log($"Armed {Name(ctx.hallway)} for back-guess. frontIsAnomaly={ctx.frontWasAnomalyAtArm}");
        }

        private void ForwardGuess(HallwayContext ctx)
        {
            if (!IsAnomaly(currentRoom))
            {
                CorrectStreak++;
            }
            else
            {
                CorrectStreak = 0;
            }

            OnStreakChanged?.Invoke(CorrectStreak);
            ctx.armedForForwardGuess = false; // consume the armed state
        }
        private void BackGuess(HallwayContext ctx)
        {
            if (IsAnomaly(currentRoom))
            {
                CorrectStreak++;
            }
            else
            {
                CorrectStreak = 0;
            }

            OnStreakChanged?.Invoke(CorrectStreak);
            ctx.armedFromForward = false; // consume the armed state
        }

        /// <summary>When BACK fires, if we were armed by a previous FORWARD, score using the cached flag.</summary>
        private void ScoreIfBackFollowsForward(HallwayContext ctx)
        {
            if (!ctx.armedFromForward) return;

            if (ctx.frontWasAnomalyAtArm)
            {
                CorrectStreak++;
               // if (verboseLogs) Log($"✅ Correct guess on {Name(ctx.hallway)}. Streak = {CorrectStreak}");
            }
            else
            {
                //if (CorrectStreak != 0 && verboseLogs) Log($"❌ Wrong guess on {Name(ctx.hallway)}. Streak reset.");
                CorrectStreak = 0;
            }

            OnStreakChanged?.Invoke(CorrectStreak);
            ctx.armedFromForward = false; // consume the armed state
        }


        /// <summary>When BACK fires, if we were armed by a previous FORWARD, score using the cached flag.</summary>
        private void ScoreIfForwardFollowsRoom(HallwayContext ctx)
        {
            if (!ctx.armedForForwardGuess) return;

            if (!ctx.frontWasAnomalyAtArm)
            {
                CorrectStreak++;
                //if (verboseLogs) Log($"✅ Correct guess on {Name(ctx.hallway)}. Streak = {CorrectStreak}");
            }
            else
            {
                //if (CorrectStreak != 0 && verboseLogs) Log($"❌ Wrong guess on {Name(ctx.hallway)}. Streak reset.");
                CorrectStreak = 0;
            }

            OnStreakChanged?.Invoke(CorrectStreak);
            ctx.armedForForwardGuess = false; // consume the armed state
        }



        // =========================================================
        // ===================  STARTUP HELPERS  ===================
        // =========================================================

        private void PlaceStartAndFirstHallway()
        {
            // Spawn Start room
            _startRoom = socketSystem.SpawnRoom(
                startRoomPrefab,
                startPosition,
                Quaternion.LookRotation(startForward == Vector3.zero ? Vector3.forward : startForward, Vector3.up)
            );

            // Attach first hallway in front of Start by position
            var startSocket = ChooseSocketByPosition(_startRoom, _startRoom.transform.forward, mustBeFree: false);
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

            // Seed context for the first hallway
            var hctx = CreateHallwayContext(firstHall);
            hctx.backRoom = _startRoom; // purely informational; reset will clear when you move

            if (verboseLogs) Log($"Init OK. Start='{_startRoom.name}', FirstHall='{firstHall.name}'");
        }

        // =========================================================
        // ==================  ATTACH / EXTEND  ====================
        // =========================================================

        /// <summary>Attach a content room onto a known socket.</summary>
        private bool AttachContentAtSocket(Socket targetSocket, RoomSockets contentPrefab, out RoomSockets placedRoom)
        {
            placedRoom = null;
            if (!contentPrefab) return false;
            return socketSystem.TryAttachRoomToSocket(contentPrefab, targetSocket, s => true, out placedRoom);
        }

        /// <summary>
        /// From an already placed room, pick an exit socket along a direction and attach a hallway.
        /// The new hallway is registered with its own context.
        /// </summary>
        private void ExtendWithHallwayFromRoom(RoomSockets fromRoom, Vector3 extendDirection, RoomSockets ownerHallway, out RoomSockets newHallway)
        {
            newHallway = null;

            var exitSocket =
                ChooseSocketByPosition(fromRoom, extendDirection, mustBeFree: true) ??
                fromRoom.FreeSockets().FirstOrDefault();

            if (exitSocket == null)
            {
                if (verboseLogs) LogWarn($"No exit socket found on '{fromRoom.name}' to extend hallway.");
                return;
            }

            if (!socketSystem.TryAttachRoomToSocket(hallwayPrefab, exitSocket, s => true, out var hall))
            {
                if (verboseLogs) LogWarn($"Failed to attach hallway to exit socket of '{fromRoom.name}'.");
                return;
            }

            newHallway = hall;

            // Register the new hallway so future resets can find & clear it
            CreateHallwayContext(hall);

            if (verboseLogs) Log($"Extended from '{fromRoom.name}' with hallway '{hall.name}'.");
        }

        // =========================================================
        // ======================  CONTEXT  ========================
        // =========================================================

        private HallwayContext EnsureContext(RoomSockets hallway)
        {
            if (!_hallways.TryGetValue(hallway, out var ctx))
            {
                ctx = CreateHallwayContext(hallway);
            }
            else
            {
                // Re-cache sockets if missing
                if (!ctx.forwardSocket) ctx.forwardSocket = FindForwardSocket(hallway);
                if (!ctx.backSocket) ctx.backSocket = FindBackSocket(hallway);
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
        // ====================  SOCKET PICKERS  ===================
        // =========================================================

        private Socket FindForwardSocket(RoomSockets hallway)
            => ChooseSocketByPosition(hallway, hallway.transform.forward, mustBeFree: false);

        private Socket FindBackSocket(RoomSockets hallway)
            => ChooseSocketByPosition(hallway, -hallway.transform.forward, mustBeFree: false);

        /// <summary>
        /// Pick the socket whose *position* lies most along worldDir from room origin.
        /// (More robust than relying on each socket’s own Forward.)
        /// </summary>
        private Socket ChooseSocketByPosition(RoomSockets room, Vector3 worldDir, bool mustBeFree)
        {
            if (!room || room.Sockets == null) return null;

            worldDir = (worldDir == Vector3.zero ? room.transform.forward : worldDir.normalized);
            Vector3 origin = room.transform.position;

            Socket best = null;
            float bestDot = float.NegativeInfinity;

            foreach (var s in room.Sockets)
            {
                if (s == null) continue;
                if (mustBeFree && s.Occupied) continue;

                Vector3 dirFromCenter = s.transform.position - origin;
                if (dirFromCenter.sqrMagnitude < 1e-6f) continue;

                float d = Vector3.Dot(dirFromCenter.normalized, worldDir);
                if (d > bestDot)
                {
                    bestDot = d;
                    best = s;
                }
            }

            return best;
        }

        // =========================================================
        // ===================  SELECTION / RNG  ===================
        // =========================================================

        private (RoomSockets prefab, bool isEnd, bool isAnomaly) ChooseNextContent()
        {
            // End gate
            if (!_endPlaced && streakToRevealEnd > 0 && CorrectStreak >= streakToRevealEnd && endRoomPrefab)
                return (endRoomPrefab, true, false);

            // Clean vs anomaly
            double roll = socketSystem?.Rng?.NextDouble() ?? UnityEngine.Random.value;
            bool chooseClean = (roll < cleanProbability) && cleanRoomPrefab;

            if (chooseClean) return (cleanRoomPrefab, false, false);

            var anomaly = RandomAnomalyPrefab();
            if (!anomaly && cleanRoomPrefab) return (cleanRoomPrefab, false, false); // fallback

            return (anomaly, false, true);
        }

        private RoomSockets RandomAnomalyPrefab()
        {
            if (anomalyRoomPrefabs == null || anomalyRoomPrefabs.Count == 0) return null;
            int idx = socketSystem?.Rng != null
                ? socketSystem.Rng.Next(0, anomalyRoomPrefabs.Count)
                : UnityEngine.Random.Range(0, anomalyRoomPrefabs.Count);
            return anomalyRoomPrefabs[idx];
        }

        // =========================================================
        // =====================  UTIL / SAFETY  ===================
        // =========================================================

        private bool ValidateTriggerInput(RoomSockets hallway)
        {
            if (hallway) return true;
            LogWarn("Trigger called with null hallway.");
            return false;
        }

        private void TryDestroyRoom(RoomSockets room)
        {
            if (!room) return;
            try { socketSystem.DestroyRoom(room); }
            catch (Exception e)
            {
                LogWarn($"DestroyRoom exception on '{room.name}': {e.Message}");
                // fallback if needed: Destroy(room.gameObject);
            }
        }

        private void SafeFreeSocket(Socket socket)
        {
            if (!socket) return;
            try { socket.MarkOccupied(false); } catch { /* ok if not supported */ }
        }

        private static bool IsAnomaly(RoomSockets room)
        {
            var tag = room ? room.GetComponent<AnomalyTag>() : null;
            return tag && tag.isAnomaly;
        }

        private static string Name(RoomSockets r) => r ? r.name : "<null>";
        private void Log(string msg) => Debug.Log($"[AnomalyGen] {msg}");
        private void LogWarn(string msg) => Debug.LogWarning($"[AnomalyGen] {msg}");
    }

    // ----------------------------- Context -----------------------------
    internal class HallwayContext
    {
        public RoomSockets hallway;

        public Socket forwardSocket;      // best forward-positioned socket
        public Socket backSocket;         // best back-positioned socket

        public RoomSockets frontRoom;     // room currently attached forward (if any)
        public RoomSockets backRoom;      // room currently attached back (if any)

        public bool forwardGenerated;     // not strictly needed now, but kept for clarity

        // Scoring flags (no room-enter involved)
        public bool armedFromForward = false;
        public bool frontWasAnomalyAtArm = false;


        public bool armedForForwardGuess = false;
    }
}
