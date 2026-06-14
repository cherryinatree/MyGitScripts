using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProcGen.Sockets
{
    [DisallowMultipleComponent]
    public class RoomSockets : MonoBehaviour
    {
        [Header("Auto-Discovered")]
        [SerializeField, ReadOnlyInspector] private List<Socket> sockets = new();

        [Header("Bounds")]
        [Tooltip("Optional explicit bounds collider(s) to use for overlap checks. If empty, Renderers/Colliders are scanned.")]
        public List<Collider> boundsColliders = new();

        [Tooltip("Optional padding added to computed bounds (world space).")]
        public Vector3 boundsPadding = new(0.2f, 0.2f, 0.2f);

        public IReadOnlyList<Socket> Sockets => sockets;

        private void Reset() => RefreshSockets();
        private void Awake() => RefreshSockets();
        private void OnValidate() => RefreshSockets();

        public void RefreshSockets()
        {
            sockets = GetComponentsInChildren<Socket>(true).ToList();
            foreach (var s in sockets)
                s.SetOwner(this);
        }

        public IEnumerable<Socket> FreeSockets(System.Predicate<Socket> filter = null)
        {
            foreach (var s in sockets)
            {
                if (!s.Occupied && (filter == null || filter(s)))
                    yield return s;
            }
        }

        /// <summary>Combine either explicit boundsColliders or all Renderers/Colliders under this room.</summary>
        public Bounds ComputeWorldBounds()
        {
            bool haveAny = false;
            var b = new Bounds(transform.position, Vector3.one * 0.01f);

            IEnumerable<Bounds> sourceBounds()
            {
                if (boundsColliders != null && boundsColliders.Count > 0)
                {
                    foreach (var c in boundsColliders.Where(c => c)) yield return c.bounds;
                }
                else
                {
                    foreach (var r in GetComponentsInChildren<Renderer>()) yield return r.bounds;
                    foreach (var c in GetComponentsInChildren<Collider>()) yield return c.bounds;
                }
            }

            foreach (var sb in sourceBounds())
            {
                if (!haveAny) { b = sb; haveAny = true; }
                else b.Encapsulate(sb);
            }

            if (!haveAny) b = new Bounds(transform.position, Vector3.one * 0.5f);
            b.Expand(boundsPadding);
            return b;
        }
    }
}
