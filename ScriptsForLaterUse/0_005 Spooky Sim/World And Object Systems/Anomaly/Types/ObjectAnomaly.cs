using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Unity.VisualScripting.Member;

namespace Cherry.Anomalies
{
    public enum ObjectAnomalyType
    {
        Missing,
        WrongSize,
        WrongPlace,
        WrongAngle,
        ExtraObject
    }

    [System.Serializable]
    public class ObjectAnomalyVariant
    {
        public ObjectAnomalyType type;

        [Header("Offsets")]
        public Vector3 positionOffset;
        public Vector3 rotationOffsetEuler;
        public Vector3 scaleMultiplier = Vector3.one;

        [Header("Extra Object (if type = ExtraObject)")]
        public GameObject extraPrefab;

        [Header("Optional")]
        [Tooltip("If true, variant is eligible for random selection.")]
        public bool allowRandom = true;

        [Tooltip("Higher = more likely when picking random among eligible variants.")]
        public float weight = 1f;
    }

    /// <summary>
    /// Attach to any prop that can become 'wrong' during a RoomDifferent anomaly.
    /// Holds its own variants and exposes public methods to apply/restore them.
    /// </summary>
    public class ObjectAnomaly : AnomalyBase
    {
        [Header("Variants")]
        [SerializeField] private List<ObjectAnomalyVariant> variants = new();

        [Header("Behavior")]
        [SerializeField] private bool cacheNormalOnAwake = true;

        // Normal (baseline) state
        private Vector3 normalLocalPos;
        private Quaternion normalLocalRot;
        private Vector3 normalLocalScale;
        private bool normalActive;

        // Runtime applied state
        private ObjectAnomalyVariant currentVariant;
        private GameObject spawnedExtra;

        public bool IsAnomalous => currentVariant != null;

        public ObjectAnomalyVariant CurrentVariant => currentVariant;


        protected override void Activate_Internal()
        {
        }

        protected override void Deactivate_Internal()
        {
        }

        // Example: never auto-resolves unless something else calls Resolve()
        protected override bool CheckResolved_Internal() => false;

        private void Awake()
        {
            if (cacheNormalOnAwake) CacheNormal();
        }

        /// <summary>Call this if you change the object's baseline at runtime and want that to be the new "normal".</summary>
        public void CacheNormal()
        {
            normalLocalPos = transform.localPosition;
            normalLocalRot = transform.localRotation;
            normalLocalScale = transform.localScale;
            normalActive = gameObject.activeSelf;
        }

        /// <summary>
        /// Apply a random eligible variant (optionally filtered by type).
        /// </summary>
        public void ApplyRandomVariant(ObjectAnomalyType? onlyType = null, int? seed = null)
        {
            if (variants == null || variants.Count == 0) return;

            // Eligible list
            var eligible = variants
                .Where(v => v != null && v.allowRandom)
                .Where(v => onlyType == null || v.type == onlyType.Value)
                .ToList();

            if (eligible.Count == 0) return;

            // Weighted random pick
            float total = 0f;
            foreach (var v in eligible) total += Mathf.Max(0.0001f, v.weight);

            float roll;
            if (seed.HasValue)
            {
                var rng = new System.Random(seed.Value);
                roll = (float)rng.NextDouble() * total;
            }
            else
            {
                roll = Random.value * total;
            }

            float acc = 0f;
            ObjectAnomalyVariant chosen = eligible[eligible.Count - 1];
            foreach (var v in eligible)
            {
                acc += Mathf.Max(0.0001f, v.weight);
                if (roll <= acc) { chosen = v; break; }
            }

            ApplyVariant(chosen);
        }

        /// <summary>
        /// Apply the first (or random) variant matching a type.
        /// </summary>
        public void ApplyVariant(ObjectAnomalyType type, bool randomAmongType = true)
        {
            if (variants == null || variants.Count == 0) return;

            var matches = variants.Where(v => v != null && v.type == type).ToList();
            if (matches.Count == 0) return;

            if (!randomAmongType || matches.Count == 1)
            {
                ApplyVariant(matches[0]);
                return;
            }

            ApplyVariant(matches[Random.Range(0, matches.Count)]);
        }

        /// <summary>
        /// Apply a specific variant by index in the list.
        /// </summary>
        public void ApplyVariantByIndex(int index)
        {
            if (variants == null) return;
            if (index < 0 || index >= variants.Count) return;
            ApplyVariant(variants[index]);
        }

        /// <summary>
        /// Core apply. Always restores to normal first, then applies the new variant.
        /// </summary>
        public void ApplyVariant(ObjectAnomalyVariant variant)
        {
            if (variant == null) return;

            // Ensure we have a baseline
            if (!cacheNormalOnAwake) CacheNormal();

            RestoreNormal();

            currentVariant = variant;

            switch (variant.type)
            {
                case ObjectAnomalyType.Missing:
                    gameObject.SetActive(false);
                    break;

                case ObjectAnomalyType.WrongSize:
                    transform.localScale = Vector3.Scale(normalLocalScale, variant.scaleMultiplier);
                    break;

                case ObjectAnomalyType.WrongPlace:
                    transform.localPosition = normalLocalPos + variant.positionOffset;
                    break;

                case ObjectAnomalyType.WrongAngle:
                    transform.localRotation = normalLocalRot * Quaternion.Euler(variant.rotationOffsetEuler);
                    break;

                case ObjectAnomalyType.ExtraObject:
                    if (variant.extraPrefab)
                    {
                        spawnedExtra = Instantiate(variant.extraPrefab, transform.parent);
                        spawnedExtra.transform.localPosition = normalLocalPos + variant.positionOffset;
                        spawnedExtra.transform.localRotation = normalLocalRot * Quaternion.Euler(variant.rotationOffsetEuler);
                        spawnedExtra.transform.localScale =
                            Vector3.Scale(normalLocalScale, variant.scaleMultiplier);
                    }
                    break;
            }
        }

        /// <summary>Restore baseline state and clear any spawned extras.</summary>
        public void RestoreNormal()
        {
            if (spawnedExtra)
            {
                Destroy(spawnedExtra);
                spawnedExtra = null;
            }

            transform.localPosition = normalLocalPos;
            transform.localRotation = normalLocalRot;
            transform.localScale = normalLocalScale;
            gameObject.SetActive(normalActive);

            currentVariant = null;
        }

        // --- Nice editor helpers ---
        [ContextMenu("Preview/Apply Random Variant")]
        private void EditorPreviewApplyRandom()
        {
            ApplyRandomVariant();
        }

        [ContextMenu("Preview/Restore Normal")]
        private void EditorPreviewRestore()
        {
            RestoreNormal();
        }
    }
}
