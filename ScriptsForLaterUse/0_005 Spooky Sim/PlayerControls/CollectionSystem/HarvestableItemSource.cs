using System.Collections.Generic;
using Cherry.Inventory;
using UnityEngine;

[AddComponentMenu("Items/Harvestable Item Source")]
public class HarvestableItemSource : MonoBehaviour
{
    [Header("Item")]
    public ItemDefinition item;
    [Min(0)] public int quantity = 5;
    [Min(1)] public int perPickup = 1;

    private int orgionalQuantity;

    [Header("Pickup Prefab")]
    [Tooltip("Prefab that will fly to the player. Must have a PickupMover component.")]
    public GameObject pickupPrefab;

    [Header("Spawn Settings")]
    public Vector3 localSpawnOffset = Vector3.zero;
    public bool reserveOnSpawn = false;
    [Min(0)] public float spawnCooldown = 0.1f;

    [Header("Return Path (Follow Beam)")]
    public bool followBeamPathOnReturn = true;

    [Tooltip("How far ahead along the beam path the proxy target stays.")]
    [Min(0.05f)] public float leadDistance = 1.0f;

    [Tooltip("Minimum separation between pickup and proxy target. Must be > PickupMover's arrive radius.")]
    [Min(0.01f)] public float minSeparation = 0.6f;

    [Tooltip("Only when the pickup is this close to the player/origin do we snap the proxy onto the player to allow arrival.")]
    [Min(0.01f)] public float finalSnapDistance = 0.35f;

    [Header("Visuals / Lifetime")]
    public bool scaleWithQuantity = true;
    public bool destroyWhenDepleted = true;

    private float _lastSpawnTime = -999f;

    public AudioSource audioSource;
    public AudioClip harvestSound;
    [SerializeField] private bool overridePickupMoverSettings = true;
    [SerializeField, Min(0.01f)] private float forcedTravelSpeed = 8f;
    [SerializeField, Min(0f)] private float forcedMinTravelTime = 0.12f;
    [SerializeField, Min(0.001f)] private float forcedArriveDistance = 0.05f;
    [SerializeField] private bool forcedHomeToMovingTarget = true;
    private void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        orgionalQuantity = Mathf.Max(1, quantity);
    }

    // Backward-compatible
    public bool TryHarvestFromBeam(Vector3 hitPoint, Transform beamOrigin)
        => TryHarvestFromBeam(hitPoint, beamOrigin, null);

    // Path-aware overload
    public bool TryHarvestFromBeam(Vector3 hitPoint, Transform beamOrigin, IReadOnlyList<Vector3> returnPath)
    {
        if (quantity < perPickup) return false;
        if (pickupPrefab == null) { Debug.LogWarning($"{name}: No pickupPrefab assigned."); return false; }
        if (Time.time - _lastSpawnTime < spawnCooldown) return false;

        _lastSpawnTime = Time.time;

        if (beamOrigin == null)
        {
            var cm = Camera.main;
            beamOrigin = cm != null ? cm.transform : transform;
        }

        var spawnPos = hitPoint + transform.TransformVector(localSpawnOffset);
        var go = Instantiate(pickupPrefab, spawnPos, Quaternion.identity);

        var mover = go.GetComponent<PickupMover>(); 
        if (overridePickupMoverSettings && mover != null)
        {
            mover.travelSpeed = forcedTravelSpeed;
            mover.minTravelTime = forcedMinTravelTime;
            mover.arriveDistance = forcedArriveDistance;
            mover.homeToMovingTarget = forcedHomeToMovingTarget;
        }
        if (mover == null)
        {
            Debug.LogError($"{name}: pickupPrefab requires a PickupMover component.");
            Destroy(go);
            return false;
        }

        BeamReturnTargetProxy proxy = null;
        Transform moverTarget = beamOrigin;

        bool hasPath = followBeamPathOnReturn && returnPath != null && returnPath.Count >= 2;
        if (hasPath)
        {
            var proxyGO = new GameObject("BeamPickupReturnProxy");
            proxyGO.hideFlags = HideFlags.HideAndDontSave;

            // Parent under real origin so PickupMover can still find player/inventory via GetComponentInParent/root/tag logic
            proxyGO.transform.SetParent(beamOrigin, worldPositionStays: true);
            proxyGO.layer = beamOrigin.gameObject.layer;
            try { proxyGO.tag = beamOrigin.gameObject.tag; } catch { }

            proxy = proxyGO.AddComponent<BeamReturnTargetProxy>();
            proxy.Initialize(
                pickup: go.transform,
                finalTarget: beamOrigin,
                pathPoints: returnPath,
                lead: leadDistance,
                minSep: minSeparation,
                snapDistance: finalSnapDistance
            );

            moverTarget = proxy.transform;
        }

        if (reserveOnSpawn)
        {
            quantity -= perPickup;
            ApplyScale();
            if (destroyWhenDepleted && quantity <= 0)
                Destroy(gameObject);
        }

        mover.Initialize(
            item: item,
            amount: perPickup,
            target: moverTarget,
            onArrive: (success) =>
            {
                if (proxy != null) Destroy(proxy.gameObject);

                if (!this) return;

                if (success)
                {
                    if (!reserveOnSpawn)
                    {
                        quantity -= perPickup;
                        ApplyScale();
                        if (destroyWhenDepleted && quantity <= 0)
                            Destroy(gameObject);
                    }
                }
                else
                {
                    if (reserveOnSpawn)
                    {
                        quantity += perPickup;
                        ApplyScale();
                    }
                }
            }
        );

        if (audioSource != null && harvestSound != null)
            audioSource.PlayOneShot(harvestSound);

        return true;
    }

    private void ApplyScale()
    {
        if (!scaleWithQuantity) return;
        if (orgionalQuantity <= 0) return;

        float t = Mathf.Clamp01((float)quantity / orgionalQuantity);
        transform.localScale = Vector3.one * t;
    }

    /// <summary>
    /// Proxy Transform that stays ahead of the pickup along the polyline return path,
    /// and does NOT snap to the final target until pickup is very close.
    /// This keeps pickup speed consistent across all legs.
    /// </summary>
    private class BeamReturnTargetProxy : MonoBehaviour
    {
        private readonly List<Vector3> _path = new();

        private Transform _pickup;
        private Transform _finalTarget;

        private float _lead;
        private float _minSep;
        private float _snapDist;

        private const float DEDUP_EPS = 0.001f;

        public void Initialize(
            Transform pickup,
            Transform finalTarget,
            IReadOnlyList<Vector3> pathPoints,
            float lead,
            float minSep,
            float snapDistance)
        {
            _pickup = pickup;
            _finalTarget = finalTarget;

            _minSep = Mathf.Max(0.01f, minSep);
            _lead = Mathf.Max(0.05f, lead);
            if (_lead < _minSep) _lead = _minSep; // ensure lead >= minSep
            _snapDist = Mathf.Max(0.01f, snapDistance);

            _path.Clear();

            if (_pickup == null) return;

            // Copy & dedup
            float eps2 = DEDUP_EPS * DEDUP_EPS;
            for (int i = 0; i < pathPoints.Count; i++)
            {
                Vector3 p = pathPoints[i];
                if (_path.Count == 0 || (_path[_path.Count - 1] - p).sqrMagnitude > eps2)
                    _path.Add(p);
            }

            // Fallback if path is invalid
            if (_path.Count < 2 && _finalTarget != null)
            {
                _path.Clear();
                _path.Add(_pickup.position);
                _path.Add(_finalTarget.position);
            }

            transform.position = _pickup.position;
        }

        private void Update()
        {
            if (_pickup == null || _finalTarget == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 finalPos = _finalTarget.position;

            if (_path == null || _path.Count < 2)
            {
                // If no path, behave like direct to final
                transform.position = finalPos;
                return;
            }

            // Find closest point along polyline to pickup, expressed as arc-length s
            float bestDist2 = float.PositiveInfinity;
            float bestS = 0f;

            float cumS = 0f;

            for (int i = 0; i < _path.Count - 1; i++)
            {
                Vector3 a = _path[i];
                Vector3 b = _path[i + 1];

                // Last segment uses dynamic final target position
                if (i == _path.Count - 2)
                    b = finalPos;

                Vector3 ab = b - a;
                float ab2 = ab.sqrMagnitude;
                if (ab2 < 0.0000001f) continue;

                float t = Mathf.Clamp01(Vector3.Dot(_pickup.position - a, ab) / ab2);
                Vector3 proj = a + ab * t;

                float d2 = (proj - _pickup.position).sqrMagnitude;

                float segLen = Mathf.Sqrt(ab2);
                float sHere = cumS + (segLen * t);

                if (d2 < bestDist2)
                {
                    bestDist2 = d2;
                    bestS = sHere;
                }

                cumS += segLen;
            }

            float totalLen = cumS;

            // Choose a target arc-length ahead
            float targetS = bestS + _lead;

            // Ensure proxy isn't too close (prevents early arrive checks)
            float distProxyNow = Vector3.Distance(transform.position, _pickup.position);
            if (distProxyNow < _minSep)
                targetS += (_minSep - distProxyNow);

            // If targetS would go past the end, DON'T snap to final yet.
            if (targetS >= totalLen)
            {
                float distToFinal = Vector3.Distance(_pickup.position, finalPos);

                // Only now do we let PickupMover truly "arrive"
                if (distToFinal <= _snapDist)
                {
                    transform.position = finalPos;
                    return;
                }

                // Keep proxy ahead toward final, but never closer than snap distance
                if (distToFinal < 0.0001f)
                {
                    transform.position = finalPos;
                    return;
                }

                Vector3 dir = (finalPos - _pickup.position) / distToFinal;

                float maxAhead = Mathf.Max(0f, distToFinal - _snapDist); // don't push past the "arrival gate"
                float desiredAhead = Mathf.Min(_lead, maxAhead);
                desiredAhead = Mathf.Max(desiredAhead, Mathf.Min(_minSep, maxAhead)); // keep some separation if possible

                transform.position = _pickup.position + dir * desiredAhead;
                return;
            }

            // Convert target arc-length to world point on polyline
            float walk = targetS;

            for (int i = 0; i < _path.Count - 1; i++)
            {
                Vector3 a = _path[i];
                Vector3 b = _path[i + 1];
                if (i == _path.Count - 2)
                    b = finalPos;

                float segLen = Vector3.Distance(a, b);
                if (segLen < 0.000001f) continue;

                if (walk <= segLen)
                {
                    float t = walk / segLen;
                    transform.position = Vector3.Lerp(a, b, t);
                    return;
                }

                walk -= segLen;
            }

            // Fallback
            transform.position = finalPos;
        }
    }
}