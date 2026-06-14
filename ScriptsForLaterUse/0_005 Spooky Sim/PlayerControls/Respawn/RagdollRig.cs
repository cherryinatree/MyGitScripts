using UnityEngine;

namespace Cherry.Animation
{
    [DisallowMultipleComponent]
    public class RagdollRig : MonoBehaviour
    {
        [Header("Optional refs")]
        [SerializeField] private Animator animator;

        [Tooltip("Optional: your main movement Rigidbody (root). Excluded from ragdoll bones.")]
        [SerializeField] private Rigidbody movementRigidbody;

        [Tooltip("Optional: your main movement collider (root). Excluded from ragdoll bones.")]
        [SerializeField] private Collider movementCollider;

        [Header("Auto-found bones")]
        [SerializeField] private Rigidbody[] boneBodies;
        [SerializeField] private Collider[] boneColliders;

        public bool IsRagdoll { get; private set; }

        /// <summary>True if we have any bone rigidbodies/colliders to simulate.</summary>
        public bool CanRagdoll => boneBodies != null && boneBodies.Length > 0 && boneColliders != null && boneColliders.Length > 0;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (movementRigidbody == null) movementRigidbody = GetComponent<Rigidbody>();
            if (movementCollider == null) movementCollider = GetComponent<Collider>();

            CacheBones();
            // Start non-ragdoll safely (even if bones are missing)
            TrySetRagdoll(false);
        }

        private void CacheBones()
        {
            var allBodies = GetComponentsInChildren<Rigidbody>(true);
            var allCols = GetComponentsInChildren<Collider>(true);

            var bodies = new System.Collections.Generic.List<Rigidbody>(allBodies.Length);
            foreach (var rb in allBodies)
            {
                if (rb == null) continue;
                if (movementRigidbody != null && rb == movementRigidbody) continue;
                bodies.Add(rb);
            }
            boneBodies = bodies.ToArray();

            var cols = new System.Collections.Generic.List<Collider>(allCols.Length);
            foreach (var col in allCols)
            {
                if (col == null) continue;
                if (movementCollider != null && col == movementCollider) continue;
                cols.Add(col);
            }
            boneColliders = cols.ToArray();
        }

        /// <summary>
        /// Returns false if ragdoll isn't set up (bones missing). Never leaves you stuck.
        /// </summary>
        public bool TrySetRagdoll(bool enable)
        {
            if (enable && !CanRagdoll)
            {
                Debug.LogWarning($"{name}: Ragdoll requested, but no bone bodies/colliders found. Skipping ragdoll.");
                return false;
            }

            IsRagdoll = enable;

            if (animator != null) animator.enabled = !enable;

            // If you have a movement RB controller:
            if (movementRigidbody != null)
            {
                movementRigidbody.isKinematic = enable;
                movementRigidbody.detectCollisions = !enable;

                if (!enable)
                {
                    // Clear leftover momentum
                    movementRigidbody.linearVelocity = Vector3.zero;
                    movementRigidbody.angularVelocity = Vector3.zero;
                }
            }

            if (movementCollider != null)
                movementCollider.enabled = !enable;

            // Bones (only meaningful if CanRagdoll)
            if (CanRagdoll)
            {
                for (int i = 0; i < boneBodies.Length; i++)
                {
                    var rb = boneBodies[i];
                    if (rb == null) continue;
                    rb.isKinematic = !enable;
                    rb.detectCollisions = enable;
                }

                for (int i = 0; i < boneColliders.Length; i++)
                {
                    var col = boneColliders[i];
                    if (col == null) continue;
                    col.enabled = enable;
                }
            }

            return true;
        }

        public void AddImpulse(Vector3 worldDir, float force)
        {
            if (!IsRagdoll || !CanRagdoll) return;

            for (int i = 0; i < boneBodies.Length; i++)
            {
                var rb = boneBodies[i];
                if (rb == null || rb.isKinematic) continue;
                rb.AddForce(worldDir * force, ForceMode.Impulse);
            }
        }
    }
}
