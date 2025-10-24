using UnityEngine;

namespace ProcGen.Sockets
{
    /// <summary>
    /// Aligns and connects two sockets by rotating & moving the CANDIDATE room so that:
    /// candidateSocket.forward == -targetSocket.forward AND positions coincide,
    /// then applies per-socket connection offsets. Optional overlap check.
    /// </summary>
    public class SocketConnector : MonoBehaviour
    {
        [Header("Overlap Check")]
        public bool checkOverlap = true;
        public LayerMask overlapMask = ~0; // everything by default
        [Tooltip("If > 0, uses NonAlloc buffer of this size for overlap tests (small GC if 0).")]
        public int overlapBuffer = 64;
        [Tooltip("Gizmo for prospective bounds on connect test.")]
        public bool drawBoundsGizmos = false;

        private Collider[] _buffer;

        /// <summary>Try to connect two sockets. Candidate's room root will be moved/rotated to match target.</summary>
        public bool TryConnect(Socket target, Socket candidate, out Quaternion appliedRotation, out Vector3 appliedTranslation, out Bounds candidateWorldBounds)
        {
            appliedRotation = Quaternion.identity;
            appliedTranslation = Vector3.zero;
            candidateWorldBounds = new Bounds();

            if (!target || !candidate) return false;
            if (!target.IsCompatibleWith(candidate)) return false;

            var candidateRoom = candidate.OwnerRoom;
            var targetRoom = target.OwnerRoom;
            if (!candidateRoom || !targetRoom) return false;

            // 1) Compute the root rotation that makes candidate.forward == -target.forward
            var desiredSocketRot = Quaternion.LookRotation(-target.Forward, target.transform.up);
            // socket's current rotation RELATIVE to its room root:
            var socketLocalRot = Quaternion.Inverse(candidateRoom.transform.rotation) * candidate.transform.rotation;
            var newRootRot = desiredSocketRot * Quaternion.Inverse(socketLocalRot);

            // 2) Apply rotation
            var root = candidateRoom.transform;
            root.rotation = newRootRot;
            appliedRotation = newRootRot;

            // 3) Move so the socket pivots coincide (after rotation)
            var worldCandidatePivot = candidate.transform.position;
            var worldTargetPivot = target.transform.position;
            var delta = worldTargetPivot - worldCandidatePivot;
            root.position += delta;

            // 4) Apply per-socket connection offsets (expressed in their own local spaces)
            var targetOffsetWorld = target.transform.TransformVector(target.connectionOffsetLocal);
            var candidateOffsetWorld = candidate.transform.TransformVector(candidate.connectionOffsetLocal);
            var totalOffset = targetOffsetWorld - candidateOffsetWorld;
            root.position += totalOffset;
            appliedTranslation = delta + totalOffset;

            // 5) (Optional) Overlap test for the newly placed candidate room
            candidateWorldBounds = candidateRoom.ComputeWorldBounds();

            if (checkOverlap)
            {
                if (_buffer == null || _buffer.Length != overlapBuffer) _buffer = overlapBuffer > 0 ? new Collider[overlapBuffer] : null;

                bool overlaps;
                if (_buffer != null && _buffer.Length > 0)
                {
                    var count = Physics.OverlapBoxNonAlloc(candidateWorldBounds.center, candidateWorldBounds.extents, _buffer, Quaternion.identity, overlapMask);
                    overlaps = HasBlockingOverlap(_buffer, count, candidateRoom, targetRoom);
                }
                else
                {
                    var hits = Physics.OverlapBox(candidateWorldBounds.center, candidateWorldBounds.extents, Quaternion.identity, overlapMask);
                    overlaps = HasBlockingOverlap(hits, hits.Length, candidateRoom, targetRoom);
                }

                if (overlaps)
                {
                    // revert? — we leave it to caller to destroy or reposition the room
                    return false;
                }
            }

            // 6) Mark occupied; caller can store a connection edge if desired
            target.MarkOccupied(true);
            candidate.MarkOccupied(true);
            return true;
        }

        private bool HasBlockingOverlap(Collider[] hits, int count, RoomSockets candidateRoom, RoomSockets targetRoom)
        {
            for (int i = 0; i < count; i++)
            {
                var h = hits[i];
                if (!h) continue;
                // Ignore colliders that belong to either the candidate or the target room
                if (h.transform.IsChildOf(candidateRoom.transform)) continue;
                if (h.transform.IsChildOf(targetRoom.transform)) continue;
                // Found an external overlap
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!drawBoundsGizmos) return;
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f);
        }
#endif
    }
}
