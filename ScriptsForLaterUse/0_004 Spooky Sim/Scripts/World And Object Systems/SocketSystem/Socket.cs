using System.Collections.Generic;
using UnityEngine;

namespace ProcGen.Sockets
{
    [DisallowMultipleComponent]
    public class Socket : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string category = "Door";       // e.g., Door, Hallway, CaveMouth, Pipe, etc.

        [Tooltip("If true, this socket accepts ANY category.")]
        [SerializeField] private bool acceptsAny = false;

        [Tooltip("If not 'acceptsAny', these are allowed categories this socket can connect TO.")]
        [SerializeField] private List<string> acceptsCategories = new() { "Door" };

        [Header("Offsets")]
        [Tooltip("Optional local-space offset from the socket pivot applied AFTER alignment.")]
        public Vector3 connectionOffsetLocal = Vector3.zero;

        [Header("State (read-only)")]
        [SerializeField, ReadOnlyInspector] private bool occupied;
        public bool Occupied => occupied;

        [SerializeField, ReadOnlyInspector] private RoomSockets ownerRoom;
        public RoomSockets OwnerRoom => ownerRoom;

        public string Category => category;

        /// <summary>Forward = socket facing. When connecting, candidate socket forward will be aligned to -target.forward.</summary>
        public Vector3 Forward => transform.forward;

        private void Awake()
        {
            if (!ownerRoom) ownerRoom = GetComponentInParent<RoomSockets>();
        }

        internal void SetOwner(RoomSockets room) => ownerRoom = room;

        public void MarkOccupied(bool value) => occupied = value;

        /// <summary>
        /// Mutual compatibility: this socket must accept other's category AND other must accept this category (unless either acceptsAny).
        /// </summary>
        public bool IsCompatibleWith(Socket other)
        {
            if (other == null || other == this) return false;
            if (occupied || other.occupied) return false;

            bool thisOk = acceptsAny || acceptsCategories.Contains(other.category);
            bool otherOk = other.acceptsAny || other.acceptsCategories.Contains(category);
            return thisOk && otherOk;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            var c = occupied ? new Color(1f, 0.4f, 0.2f, 0.9f) : new Color(0.2f, 1f, 0.6f, 0.9f);
            Gizmos.color = c;

            // Pivot
            Gizmos.DrawSphere(Vector3.zero, 0.05f);

            // Forward arrow
            Gizmos.DrawRay(Vector3.zero, Vector3.forward * 0.45f);
            Gizmos.DrawWireCube(Vector3.forward * 0.45f, new Vector3(0.1f, 0.1f, 0.1f));

            // Label
#if UNITY_EDITOR
            UnityEditor.Handles.color = c;
            UnityEditor.Handles.Label(transform.position, $"[{category}]{(occupied ? " (occ)" : "")}");
#endif
        }
#endif
    }

    /// <summary>Simple attribute to show read-only fields in inspector.</summary>
    public class ReadOnlyInspectorAttribute : PropertyAttribute { }
}
