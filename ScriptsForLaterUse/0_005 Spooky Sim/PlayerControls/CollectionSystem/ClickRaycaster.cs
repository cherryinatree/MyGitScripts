using Cherry.Combat;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[AddComponentMenu("Gameplay/Click Raycaster (Multi-Beam Optics - Multi Leg)")]
[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public class ClickRaycaster : MonoBehaviour
{
    [Header("Input (New Input System)")]
    [SerializeField] private InputActionReference clickAction;

    [Header("Ray Type")]
    [Tooltip("Starting ray type (typically Collector). Mirrors can convert it.")]
    [SerializeField] private BeamRayDefinition baseRay;

    [Header("Ray Origin & Aim")]
    [SerializeField] private Transform originOverride;
    [SerializeField] private Camera cam;
    [SerializeField, Min(0.1f)] private float maxDistance = 100f;
    [SerializeField] private LayerMask hitMask = Physics.DefaultRaycastLayers;
    [SerializeField] private QueryTriggerInteraction queryTriggers = QueryTriggerInteraction.Ignore;
    [SerializeField] private bool drawOnMiss = true;

    [Header("Optics Limits")]
    [Range(0, 16)] public int maxDepth = 4;
    [Range(1, 32)] public int maxBeams = 8;
    [SerializeField, Min(0.00001f)] private float hitSkin = 0.0005f;

    [Header("Tap vs Hold")]
    [SerializeField, Range(0.02f, 0.5f)] private float holdThreshold = 0.12f;
    [SerializeField] private bool tapToRotateOptics = true;

    [Header("Beam Animation")]
    [SerializeField, Min(0f)] private float extendDuration = 0.08f;
    [SerializeField, Min(0f)] private float retractDuration = 0.06f;
    [SerializeField] private bool retractOnRelease = true;

    [Header("Beam Visuals")]
    [SerializeField, Min(0.0005f)] private float beamWidth = 0.01f;
    [SerializeField] private Material lineMaterial;

    [Header("Battery")]
    public BatteryConsumer batteryConsumer;
    public float batteryConsumptionPerSecond = 1f;

    [Header("Events (Primary Beam Only)")] 
    public UnityEvent onAnyHit;
    [Serializable] public class GameObjectEvent : UnityEvent<GameObject> { }
    [Serializable] public class RaycastHitEvent : UnityEvent<RaycastHit> { }
    public GameObjectEvent onHitObject;
    public RaycastHitEvent onHitInfo;
    public UnityEvent onMiss;

    public AudioSource beamAudioSource;

    // ---- internals ----
    private bool pendingPress;
    private float pressTime;
    private bool beamHeld;

    private float animT; // 0..1
    private Collider lastPrimaryEndHit;

    // We pool LineRenderers per LEG now (not per path).
    private readonly List<LineRenderer> lrPool = new();
    private readonly List<BeamPath> paths = new();

    private readonly List<Vector3> tmpDirs = new();
    private readonly List<float> tmpWeights = new();
    private readonly List<BeamRayDefinition> tmpRays = new();

    // ===== Multi-leg data =====
    private class BeamLeg
    {
        public readonly List<Vector3> points = new();
        public BeamRayDefinition ray;
        public float energy = 1f;
        public float length = 0f;
    }

    private class BeamPath
    {
        public readonly List<BeamLeg> legs = new();
        public bool hitSomething;
        public RaycastHit endHit;
        public float totalLength;
        public BeamRayDefinition endRay;
        public float endEnergy;
        public readonly List<Vector3> returnPath = new();
    }

    private struct BeamNode
    {
        public List<BeamLeg> doneLegs;   // finalized legs so far (shared references ok)
        public BeamLeg currentLeg;       // mutable leg currently being extended
        public Vector3 origin;           // physics origin for next cast (may be offset)
        public Vector3 dir;
        public float remaining;
        public int depth;
        public BeamRayDefinition ray;
        public float energy;
    }

    // --- Material property ids (for ApplyLineColor helper) ---
    private static readonly int _BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int _ColorID = Shader.PropertyToID("_Color");
    private static readonly int _UnlitColorID = Shader.PropertyToID("_UnlitColor");
    private static readonly int _EmissionColorID = Shader.PropertyToID("_EmissionColor");
    private static readonly int _EmissiveColorID = Shader.PropertyToID("_EmissiveColor");

    private void Awake()
    {
        if (cam == null)
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
        }

        if (beamAudioSource == null)
            beamAudioSource = GetComponent<AudioSource>();

        if (batteryConsumer == null)
        {
            var player = GameObject.Find("Player");
            if (player != null) batteryConsumer = player.GetComponent<BatteryConsumer>();
        }

        // Pool[0] uses the existing LineRenderer
        var lr0 = GetComponent<LineRenderer>();
        ConfigureLineRenderer(lr0);
        lr0.enabled = false;
        lrPool.Add(lr0);
    }

    private void OnEnable()
    {
        if (clickAction == null || clickAction.action == null)
        {
            Debug.LogError($"{name}: ClickRaycaster needs an InputActionReference assigned.");
            return;
        }

        clickAction.action.started += OnClickStarted;
        clickAction.action.canceled += OnClickCanceled;
        clickAction.action.Enable();
    }

    private void OnDisable()
    {
        if (clickAction != null && clickAction.action != null)
        {
            clickAction.action.started -= OnClickStarted;
            clickAction.action.canceled -= OnClickCanceled;
            clickAction.action.Disable();
        }

        StopBeamImmediate();
        pendingPress = false;
    }

    private void OnClickStarted(InputAction.CallbackContext _)
    {
        if (cam == null) return;
        pendingPress = true;
        pressTime = Time.time;
    }

    private void OnClickCanceled(InputAction.CallbackContext _)
    {
        float heldFor = Time.time - pressTime;

        if (beamHeld)
        {
            EndBeam();
            pendingPress = false;
            return;
        }

        if (pendingPress && tapToRotateOptics && heldFor < holdThreshold)
            TryRotateOptic();

        pendingPress = false;
    }

    private void Update()
    {
        if (cam == null) return;

        if (beamHeld && batteryConsumer != null && !batteryConsumer.CanUse())
        {
            StopBeamImmediate();
            return;
        }

        if (pendingPress && !beamHeld && (Time.time - pressTime) >= holdThreshold)
        {
            BeginBeam();
            pendingPress = false;
        }

        if (!beamHeld && !AnyLineEnabled()) return;

        Transform beamOriginT = originOverride ? originOverride : cam.transform;
        Vector3 startOrigin = beamOriginT.position;
        Vector3 startDir = cam.transform.forward.normalized;

        BuildBeamPaths(startOrigin, startDir, baseRay);

        float dt = Time.deltaTime;

        if (beamHeld)
            animT = (extendDuration > 0f) ? Mathf.Clamp01(animT + dt / extendDuration) : 1f;
        else
        {
            if (retractOnRelease)
            {
                animT = (retractDuration > 0f) ? Mathf.Clamp01(animT - dt / retractDuration) : 0f;
                if (animT <= 0.0001f)
                {
                    DisableAllLines();
                    return;
                }
            }
            else
            {
                DisableAllLines();
                return;
            }
        }

        int totalLegs = CountTotalLegs(paths);
        EnsurePool(totalLegs);

        // Disable all first
        for (int i = 0; i < lrPool.Count; i++)
            lrPool[i].enabled = false;

        var interactionRouter = GetComponent<BeamInteractionRouter>();

        // Render and interact
        BeamPath primary = ChoosePrimary(paths);

        int lrIndex = 0;
        for (int pIndex = 0; pIndex < paths.Count; pIndex++)
        {
            var path = paths[pIndex];

            float pathDrawLen = Mathf.Lerp(0f, path.totalLength, animT);
            float remaining = pathDrawLen;

            for (int l = 0; l < path.legs.Count; l++)
            {
                if (lrIndex >= lrPool.Count) break;

                var leg = path.legs[l];
                float legDraw = Mathf.Min(remaining, leg.length);

                // Don’t draw empty legs
                if (legDraw > 0.00001f)
                {
                    var lr = lrPool[lrIndex++];
                    lr.enabled = true;

                    DrawPartialPath(lr, leg.points, legDraw);

                    Color c = (leg.ray != null) ? leg.ray.color : Color.white;

                    // Dim by energy a bit (optional)
                    float e = Mathf.Clamp01(leg.energy);
                    c.a = Mathf.Lerp(0.15f, 1f, e);

                    lr.widthMultiplier = beamWidth * Mathf.Lerp(0.6f, 1.15f, e);
                    ApplyLineColor(lr, c);
                }

                remaining -= legDraw;
                if (remaining <= 0.00001f) break;
            }

            // Interactions happen on the FINAL HIT of the path
            if (beamHeld && path.hitSomething)
            {
                if (interactionRouter != null)
                    interactionRouter.HandleHit(path.endHit, path.endRay, dt, path.endEnergy, beamOriginT, path.returnPath);
                else
                    GetComponent<BeamHitRouter>()?.HandleHit(path.endHit, path.returnPath);
            }
        }

        // Events only for primary endpoint
        if (beamHeld)
        {
            if (primary != null && primary.hitSomething)
            {
                if (lastPrimaryEndHit != primary.endHit.collider)
                {
                    lastPrimaryEndHit = primary.endHit.collider;
                    onAnyHit?.Invoke();
                    onHitObject?.Invoke(primary.endHit.collider.gameObject);
                    onHitInfo?.Invoke(primary.endHit);
                }
            }
            else
            {
                if (lastPrimaryEndHit != null)
                {
                    lastPrimaryEndHit = null;
                    onMiss?.Invoke();
                }
            }
        }
    }
    private static void BuildReturnPath(List<BeamLeg> legs, List<Vector3> outPts, float dedupEps = 0.0005f)
    {
        outPts.Clear();
        if (legs == null || legs.Count == 0) return;

        float eps2 = dedupEps * dedupEps;

        // Reverse through legs, reverse through their points
        for (int l = legs.Count - 1; l >= 0; l--)
        {
            var pts = legs[l].points;
            if (pts == null) continue;

            for (int i = pts.Count - 1; i >= 0; i--)
            {
                Vector3 p = pts[i];
                if (outPts.Count == 0 || (outPts[outPts.Count - 1] - p).sqrMagnitude > eps2)
                    outPts.Add(p);
            }
        }
    }
    private int CountTotalLegs(List<BeamPath> ps)
    {
        int total = 0;
        for (int i = 0; i < ps.Count; i++) total += ps[i].legs.Count;
        return total;
    }

    private BeamPath ChoosePrimary(List<BeamPath> list)
    {
        // Prefer highest-energy HIT (end energy); else highest end energy.
        BeamPath bestHit = null;
        float bestE = -1f;

        for (int i = 0; i < list.Count; i++)
        {
            var p = list[i];
            if (p.hitSomething && p.endEnergy > bestE)
            {
                bestE = p.endEnergy;
                bestHit = p;
            }
        }
        if (bestHit != null) return bestHit;

        BeamPath best = null;
        bestE = -1f;
        for (int i = 0; i < list.Count; i++)
        {
            var p = list[i];
            if (p.endEnergy > bestE)
            {
                bestE = p.endEnergy;
                best = p;
            }
        }
        return best;
    }

    private bool AnyLineEnabled()
    {
        for (int i = 0; i < lrPool.Count; i++)
            if (lrPool[i] != null && lrPool[i].enabled) return true;
        return false;
    }

    private void BeginBeam()
    {
        if (batteryConsumer != null)
        {
            if (!batteryConsumer.CanUse())
            {
                pendingPress = false;
                return;
            }
            batteryConsumer.StartDrain("ClickRaycaster", batteryConsumptionPerSecond);
        }

        if (beamAudioSource != null && !beamAudioSource.isPlaying)
            beamAudioSource.Play();

        beamHeld = true;
        animT = 0f;
        lastPrimaryEndHit = null;
        lrPool[0].enabled = true;
    }

    private void EndBeam()
    {
        if (beamAudioSource != null && beamAudioSource.isPlaying)
            beamAudioSource.Stop();

        if (batteryConsumer != null)
            batteryConsumer.StopDrain("ClickRaycaster");

        beamHeld = false;

        if (!retractOnRelease)
            DisableAllLines();
    }

    private void StopBeamImmediate()
    {
        if (beamAudioSource != null && beamAudioSource.isPlaying)
            beamAudioSource.Stop();

        if (batteryConsumer != null)
            batteryConsumer.StopDrain("ClickRaycaster");

        beamHeld = false;
        pendingPress = false;
        animT = 0f;
        lastPrimaryEndHit = null;

        DisableAllLines();
    }

    private void DisableAllLines()
    {
        for (int i = 0; i < lrPool.Count; i++)
            if (lrPool[i] != null) lrPool[i].enabled = false;
    }

    private void ConfigureLineRenderer(LineRenderer lr)
    {
        lr.useWorldSpace = true;
        lr.widthMultiplier = beamWidth;
        lr.positionCount = 2;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 4;

        if (lineMaterial != null)
            lr.material = new Material(lineMaterial); // unique instance
    }

    private void EnsurePool(int needed)
    {
        while (lrPool.Count < needed)
        {
            var go = new GameObject($"BeamLine_{lrPool.Count}");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            ConfigureLineRenderer(lr);
            lr.enabled = false;
            lrPool.Add(lr);
        }
    }

    private void TryRotateOptic()
    {
        Vector3 origin = originOverride ? originOverride.position : cam.transform.position;
        Vector3 dir = cam.transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, maxDistance, hitMask, queryTriggers))
        {
            var optic = hit.collider.GetComponentInParent<ReflectiveSurface>();
            if (optic != null && optic.allowTapRotate)
                optic.RotateStep(+1);
        }
    }

    private void BuildBeamPaths(Vector3 startOrigin, Vector3 startDir, BeamRayDefinition startRay)
    {
        paths.Clear();

        Stack<BeamNode> stack = new Stack<BeamNode>(maxBeams);

        var firstLeg = new BeamLeg { ray = startRay, energy = 1f };
        firstLeg.points.Add(startOrigin);

        stack.Push(new BeamNode
        {
            doneLegs = new List<BeamLeg>(4),
            currentLeg = firstLeg,
            origin = startOrigin,
            dir = startDir.normalized,
            remaining = maxDistance,
            depth = 0,
            energy = 1f,
            ray = startRay
        });

        while (stack.Count > 0 && paths.Count < maxBeams)
        {
            var node = stack.Pop();

            if (node.remaining <= 0.0001f)
            {
                FinalizePathMiss(node);
                continue;
            }

            if (Physics.Raycast(node.origin, node.dir, out RaycastHit hit, node.remaining, hitMask, queryTriggers))
            {
                // Extend current leg to the hit point (this leg keeps the current ray color)
                node.currentLeg.points.Add(hit.point);

                float newRemaining = node.remaining - hit.distance;

                var optic = hit.collider.GetComponentInParent<ReflectiveSurface>();

                // Branch if optic and depth allows
                if (optic != null && node.depth < maxDepth && newRemaining > 0.0001f)
                {
                    bool handled = optic.GetOutgoingBeams(node.ray, node.dir, hit.normal, tmpDirs, tmpWeights, tmpRays);

                    if (handled && tmpDirs.Count > 0)
                    {
                        // Finalize this leg ONCE (shared by all children)
                        FinalizeLeg(node.currentLeg);

                        for (int i = 0; i < tmpDirs.Count; i++)
                        {
                            if (paths.Count + stack.Count >= maxBeams) break;

                            Vector3 outDir = tmpDirs[i].normalized;
                            float w = (i < tmpWeights.Count) ? tmpWeights[i] : 1f;
                            if (w <= 0.0001f) continue;

                            BeamRayDefinition outRay = (i < tmpRays.Count) ? tmpRays[i] : node.ray;
                            float outEnergy = node.energy * Mathf.Clamp01(w);

                            // Child inherits done legs + the finalized incoming leg
                            var childDone = new List<BeamLeg>(node.doneLegs.Count + 1);
                            childDone.AddRange(node.doneLegs);
                            childDone.Add(node.currentLeg);

                            // NEW LEG starts at the mirror point, but physics origin is slightly offset
                            var childLeg = new BeamLeg { ray = outRay, energy = outEnergy };
                            childLeg.points.Add(hit.point);

                            stack.Push(new BeamNode
                            {
                                doneLegs = childDone,
                                currentLeg = childLeg,
                                origin = hit.point + outDir * hitSkin,
                                dir = outDir,
                                remaining = newRemaining,
                                depth = node.depth + 1,
                                energy = outEnergy,
                                ray = outRay
                            });
                        }

                        continue;
                    }
                }

                // No optic continuation: path ends on this hit
                FinalizePathHit(node, hit);
            }
            else
            {
                // Miss: extend leg to max range end
                Vector3 end = node.origin + node.dir * node.remaining;
                node.currentLeg.points.Add(end);
                FinalizePathMiss(node);
            }
        }
    }

    private void FinalizePathHit(BeamNode node, RaycastHit endHit)
    {
        FinalizeLeg(node.currentLeg);

        var path = new BeamPath();
        path.legs.AddRange(node.doneLegs);
        path.legs.Add(node.currentLeg);
        BuildReturnPath(path.legs, path.returnPath);
        path.hitSomething = true;
        path.endHit = endHit;
        path.totalLength = SumLegLengths(path.legs);
        path.endRay = node.ray;
        path.endEnergy = node.energy;

        paths.Add(path);
    }

    private void FinalizePathMiss(BeamNode node)
    {
        FinalizeLeg(node.currentLeg);

        var path = new BeamPath();
        path.legs.AddRange(node.doneLegs);
        path.legs.Add(node.currentLeg);

        path.hitSomething = false;

        // If you truly want no line when missing
        path.totalLength = drawOnMiss ? SumLegLengths(path.legs) : 0f;

        // End ray/energy still meaningful (for visuals)
        path.endRay = node.ray;
        path.endEnergy = node.energy;

        paths.Add(path);
    }

    private float SumLegLengths(List<BeamLeg> legs)
    {
        float total = 0f;
        for (int i = 0; i < legs.Count; i++) total += legs[i].length;
        return total;
    }

    private void FinalizeLeg(BeamLeg leg)
    {
        leg.length = ComputePolylineLength(leg.points);
    }

    private float ComputePolylineLength(List<Vector3> pts)
    {
        float total = 0f;
        for (int i = 0; i < pts.Count - 1; i++)
            total += Vector3.Distance(pts[i], pts[i + 1]);
        return total;
    }

    private void DrawPartialPath(LineRenderer lr, List<Vector3> fullPts, float lengthToDraw)
    {
        if (fullPts == null || fullPts.Count < 2 || lengthToDraw <= 0.00001f)
        {
            lr.positionCount = 2;
            Vector3 p = (fullPts != null && fullPts.Count > 0) ? fullPts[0] : transform.position;
            lr.SetPosition(0, p);
            lr.SetPosition(1, p);
            return;
        }

        List<Vector3> drawPts = new List<Vector3>(fullPts.Count);
        drawPts.Add(fullPts[0]);

        float remaining = Mathf.Max(0f, lengthToDraw);

        for (int i = 0; i < fullPts.Count - 1; i++)
        {
            Vector3 a = fullPts[i];
            Vector3 b = fullPts[i + 1];
            float segLen = Vector3.Distance(a, b);
            if (segLen <= 0.000001f) continue;

            if (remaining >= segLen)
            {
                drawPts.Add(b);
                remaining -= segLen;
            }
            else
            {
                float t = remaining / segLen;
                drawPts.Add(Vector3.Lerp(a, b, Mathf.Clamp01(t)));
                break;
            }
        }

        if (drawPts.Count == 1) drawPts.Add(drawPts[0]);

        lr.positionCount = drawPts.Count;
        for (int i = 0; i < drawPts.Count; i++)
            lr.SetPosition(i, drawPts[i]);
    }

    // Shader-agnostic per-leg coloring (works even if shader ignores vertex colors)
    private void ApplyLineColor(LineRenderer lr, Color c)
    {
        lr.startColor = c;
        lr.endColor = c;

        var m = lr.material;
        if (m == null) return;

        if (m.HasProperty(_BaseColorID)) m.SetColor(_BaseColorID, c);
        if (m.HasProperty(_ColorID)) m.SetColor(_ColorID, c);
        if (m.HasProperty(_UnlitColorID)) m.SetColor(_UnlitColorID, c);

        if (m.HasProperty(_EmissionColorID))
        {
            m.SetColor(_EmissionColorID, c);
            m.EnableKeyword("_EMISSION");
        }
        if (m.HasProperty(_EmissiveColorID))
        {
            m.SetColor(_EmissiveColorID, c);
            m.EnableKeyword("_EMISSION");
        }
    }
}