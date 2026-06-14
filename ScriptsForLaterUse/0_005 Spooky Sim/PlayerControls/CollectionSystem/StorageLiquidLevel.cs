using UnityEngine;

namespace Cherry.Inventory
{
    [AddComponentMenu("Cherry/Inventory/Storage Liquid Level")]
    public class StorageLiquidLevel : MonoBehaviour
    {
        public enum FillMode { Units, Slots }

        [Header("References")]
        [SerializeField] private StorageContainer container;
        [SerializeField] private Transform liquid; // the child mesh/GO representing liquid

        [Header("Fill Calculation")]
        [SerializeField] private FillMode fillMode = FillMode.Units;

        [Header("Presentation")]
        [Tooltip("If true, changes localPosition.y between EmptyY and FullY. If false, scales Y between EmptyScaleY and FullScaleY.")]
        [SerializeField] private bool usePosition = true;

        [Tooltip("Local Y when empty.")]
        [SerializeField] private float emptyY = -0.5f;

        [Tooltip("Local Y when full.")]
        [SerializeField] private float fullY = 0.5f;

        [Tooltip("Scale Y when empty (only used if usePosition = false).")]
        [SerializeField] private float emptyScaleY = 0.0f;

        [Tooltip("Scale Y when full (only used if usePosition = false).")]
        [SerializeField] private float fullScaleY = 1.0f;

        [Header("Smoothing")]
        [SerializeField] private bool smooth = true;
        [SerializeField, Min(0.01f)] private float smoothSpeed = 10f;

        private float _targetFill01;
        private Vector3 _baseLocalPos;
        private Vector3 _baseLocalScale;

        private void Awake()
        {
            if (container == null) container = GetComponent<StorageContainer>();
            if (liquid != null)
            {
                _baseLocalPos = liquid.localPosition;
                _baseLocalScale = liquid.localScale;
            }
        }

        private void OnEnable()
        {
            if (container != null)
                container.OnStorageChanged += RecomputeTarget;

            RecomputeTarget();
            ApplyImmediate(); // start at correct level
        }

        private void OnDisable()
        {
            if (container != null)
                container.OnStorageChanged -= RecomputeTarget;
        }

        private void Update()
        {
            if (liquid == null || container == null) return;

            if (!smooth)
            {
                ApplyImmediate();
                return;
            }

            if (usePosition)
            {
                var p = liquid.localPosition;
                float desiredY = Mathf.Lerp(emptyY, fullY, _targetFill01);
                p.y = Mathf.Lerp(p.y, desiredY, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
                liquid.localPosition = p;
            }
            else
            {
                var s = liquid.localScale;
                float desiredY = Mathf.Lerp(emptyScaleY, fullScaleY, _targetFill01);
                s.y = Mathf.Lerp(s.y, desiredY, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
                liquid.localScale = s;
            }
        }

        private void RecomputeTarget()
        {
            _targetFill01 = ComputeFill01();
        }

        private float ComputeFill01()
        {
            if (container == null) return 0f;

            var slots = container.Slots;
            int cap = Mathf.Max(1, container.SlotCapacity);

            if (fillMode == FillMode.Slots)
            {
                int filled = 0;
                for (int i = 0; i < slots.Count; i++)
                    if (!slots[i].IsEmpty) filled++;

                return Mathf.Clamp01(filled / (float)cap);
            }
            else // Units
            {
                int total = 0;
                for (int i = 0; i < slots.Count; i++)
                    if (!slots[i].IsEmpty) total += Mathf.Max(0, slots[i].amount);

                int max = cap * Mathf.Max(1, container.StackLimit);
                return max <= 0 ? 0f : Mathf.Clamp01(total / (float)max);
            }
        }

        private void ApplyImmediate()
        {
            if (liquid == null) return;

            if (usePosition)
            {
                var p = _baseLocalPos;
                p.y = Mathf.Lerp(emptyY, fullY, _targetFill01);
                liquid.localPosition = p;
            }
            else
            {
                var s = _baseLocalScale;
                s.y = Mathf.Lerp(emptyScaleY, fullScaleY, _targetFill01);
                liquid.localScale = s;
            }
        }
    }
}
