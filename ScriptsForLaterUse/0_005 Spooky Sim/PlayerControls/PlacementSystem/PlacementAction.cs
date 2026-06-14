using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering; // ShadowCastingMode
using System.Collections.Generic;

public class PlacementAction : PlayerAction
{
    [Header("Refs")]
    [SerializeField] private Camera viewCam;
    [SerializeField] private LayerMask floorMask;
    [SerializeField] private LayerMask wallMask;

    [Tooltip("Layers that should BLOCK placement (other placeables + environment obstacles). " +
             "Do NOT include your floor/wall layers unless you want placement to always fail.")]
    [SerializeField] private LayerMask blockMask;

    [Header("Preview (Ghost)")]
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private float maxPlaceDistance = 8f;
    [SerializeField] private float gridSize = 0.25f;
    [SerializeField] private float rotateStep = 15f;
    [SerializeField] private float raiseStep = 0.1f;

    [Header("Surface Footprint")]
    [Tooltip("Unlit Transparent material with your footprint texture (grid/stripe/etc).")]
    [SerializeField] private Material footprintMaterial;

    [Tooltip("Extra push away from the surface to avoid z-fighting.")]
    [SerializeField] private float footprintSurfaceNudge = 0.002f;

    [Tooltip("Tint when placement is valid.")]
    [SerializeField] private Color validTint = new Color(0.2f, 1f, 0.2f, 0.6f);

    [Tooltip("Tint when placement is blocked/invalid.")]
    [SerializeField] private Color invalidTint = new Color(1f, 0.2f, 0.2f, 0.6f);

    // Holding state
    private Placeable held;
    private bool heldSpawnedNow;
    private Vector3 heldOriginalPos;
    private Quaternion heldOriginalRot;

    // Ghost
    private GameObject ghost;
    private Placeable ghostPlaceable;
    private readonly List<Renderer> ghostRenderers = new(); 
    private MaterialPropertyBlock _mpb;

    // Footprint quad
    private GameObject footprint;
    private Renderer footprintRenderer;

    // Rotation/height offsets
    private float extraHeightOffset;
    private float currentYawOffset;
    private float baseYawWorld; // stable world yaw captured at pickup

    // Last surface info (for footprint + validity)
    private bool hasSurfaceHit;
    private Vector3 surfacePoint;
    private Vector3 surfaceNormal;

    // NonAlloc overlap buffer (avoid GC)
    private readonly Collider[] _overlaps = new Collider[64];

    // Shader property IDs for tinting
    private static readonly int _BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int _ColorID = Shader.PropertyToID("_Color");
    private static readonly int _TintColorID = Shader.PropertyToID("_TintColor"); 
    
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;
    [SerializeField, Range(0.90f, 1.0f)] private float overlapPadding = 0.98f;

    private Bounds _heldLocalBounds;
    private bool _hasHeldLocalBounds;


    protected override void Subscribe(CorePlayer c) { /* no-op; using direct input */ }
    protected override void Unsubscribe(CorePlayer c) { /* no-op */ }

    private void Reset() { if (!viewCam) viewCam = Camera.main; }
    private void Start()
    {
        if (!viewCam) viewCam = Camera.main;
        _mpb = new MaterialPropertyBlock();
    }
    private void Update()
    {
        if (!viewCam) return;

        // LEFT CLICK: Pick up only (never places)
        if (held == null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (TryRaycastPlaceable(out var p))
                {
                    StartHolding(p, spawnedNow: false);
                    return; // important: don't fall through this frame
                }
            }

            return; // not holding, nothing else to do
        }

        // Holding -> rotate/preview
        if (Keyboard.current.qKey.wasPressedThisFrame) currentYawOffset -= rotateStep;
        if (Keyboard.current.eKey.wasPressedThisFrame) currentYawOffset += rotateStep;

        UpdateGhostPoseAndFootprint();

        // RIGHT CLICK: Place attempt only
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            // If invalid, TryCommitGhost() returns false and we KEEP HOLDING (do nothing)
            if (TryCommitGhost())
                StopHolding();
        }

        // ESC: Cancel holding (restore original or delete if spawned)
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelHolding();
        }
    }


    // --- API you can call from shop UI to start placing a prefab directly ---
    public void StartPlacingFromPrefab(GameObject prefab)
    {
        var instance = Instantiate(prefab);
        var placeable = instance.GetComponent<Placeable>();
        if (!placeable) placeable = instance.AddComponent<Placeable>();
        StartHolding(placeable, spawnedNow: true);
    }

    // ---------- Internals ----------
    private bool TryRaycastPlaceable(out Placeable placeable)
    {
        placeable = null;
        var ray = viewCam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        if (Physics.Raycast(ray, out var hit, maxPlaceDistance))
        {
            placeable = hit.collider.GetComponentInParent<Placeable>();
            return placeable != null;
        }
        return false;
    }

    private void StartHolding(Placeable p, bool spawnedNow)
    {
        held = p;
        heldSpawnedNow = spawnedNow;

        heldOriginalPos = held.transform.position;
        heldOriginalRot = held.transform.rotation;

        // Capture stable world yaw at pickup (so turning the player/camera won't rotate it)
        baseYawWorld = held.transform.eulerAngles.y;
        currentYawOffset = 0f;
        extraHeightOffset = 0f;

        // Create ghost clone
        ghost = Instantiate(held.gameObject);
        ghost.name = held.name + " (Ghost)";

        ghostPlaceable = ghost.GetComponent<Placeable>();

        // Disable ghost colliders so it doesn't block raycasts/physics
        foreach (var c in ghost.GetComponentsInChildren<Collider>(true))
            c.enabled = false;

        // Swap to ghost material
        ghostRenderers.Clear();
        foreach (var r in ghost.GetComponentsInChildren<Renderer>(true))
        {
            ghostRenderers.Add(r);

            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = ghostMaterial;
            r.sharedMaterials = mats;

            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        // Create footprint quad
        CreateFootprint();
        _hasHeldLocalBounds = TryComputeLocalBoundsFromColliders(held.transform, out _heldLocalBounds);

        // Fallback (rare): if no colliders, fall back to renderer bounds you already had
        if (!_hasHeldLocalBounds)
        {
            _heldLocalBounds = GetGhostLocalBounds();
            _hasHeldLocalBounds = true;
        }

        // Hide the real object while holding
        held.gameObject.SetActive(false);
    }

    private bool TryComputeLocalBoundsFromColliders(Transform root, out Bounds localB)
    {
        localB = default;
        var cols = root.GetComponentsInChildren<Collider>(true);
        if (cols == null || cols.Length == 0) return false;

        var w2l = root.worldToLocalMatrix;
        bool init = false;

        for (int i = 0; i < cols.Length; i++)
        {
            var c = cols[i];
            if (!c || !c.enabled) continue;

            var b = c.bounds; // world AABB (conservative, safe)
            Vector3 min = b.min;
            Vector3 max = b.max;

            // 8 corners of the world AABB
            for (int xi = 0; xi < 2; xi++)
                for (int yi = 0; yi < 2; yi++)
                    for (int zi = 0; zi < 2; zi++)
                    {
                        Vector3 cornerWorld = new Vector3(
                            xi == 0 ? min.x : max.x,
                            yi == 0 ? min.y : max.y,
                            zi == 0 ? min.z : max.z
                        );

                        Vector3 cornerLocal = w2l.MultiplyPoint3x4(cornerWorld);

                        if (!init)
                        {
                            localB = new Bounds(cornerLocal, Vector3.zero);
                            init = true;
                        }
                        else localB.Encapsulate(cornerLocal);
                    }
        }

        return init;
    }


    private void StopHolding()
    {
        if (ghost) Destroy(ghost);
        if (footprint) Destroy(footprint);

        ghost = null;
        ghostPlaceable = null;
        held = null;
        heldSpawnedNow = false;
    }

    private void CancelHolding()
    {
        if (!held) { StopHolding(); return; }

        if (heldSpawnedNow)
        {
            // If it was spawned from UI and you cancel, just delete it
            Destroy(held.gameObject);
        }
        else
        {
            // Restore original transform
            held.transform.SetPositionAndRotation(heldOriginalPos, heldOriginalRot);
            held.gameObject.SetActive(true);
            held.NotifyPlacedOrMoved();
        }

        StopHolding();
    }

    private void CreateFootprint()
    {
        if (!footprintMaterial) return;

        footprint = GameObject.CreatePrimitive(PrimitiveType.Quad);
        footprint.name = held.name + " (Footprint)";

        // Remove collider so it never interferes
        var col = footprint.GetComponent<Collider>();
        if (col) Destroy(col);

        footprintRenderer = footprint.GetComponent<Renderer>();
        footprintRenderer.sharedMaterial = footprintMaterial;
        footprintRenderer.shadowCastingMode = ShadowCastingMode.Off;
        footprintRenderer.receiveShadows = false;

        footprint.layer = LayerMask.NameToLayer("Ignore Raycast"); // safe default if it exists
    }

    private void UpdateGhostPoseAndFootprint()
    {
        if (!ghost || !held) return;

        hasSurfaceHit = false;

        Vector3 pos = ghost.transform.position;
        Quaternion rot = ghost.transform.rotation;

        var kind = held.placementType;
        var ray = viewCam.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        if (kind == Placeable.PlacementType.Wall)
        {
            if (Physics.Raycast(ray, out var hit, maxPlaceDistance, wallMask, QueryTriggerInteraction.Ignore))
            {
                hasSurfaceHit = true;
                surfacePoint = hit.point;
                surfaceNormal = hit.normal;

                // Face the wall normal (camera turning doesn't matter; only the hit normal matters)
                rot = Quaternion.LookRotation(-surfaceNormal, Vector3.up) * Quaternion.Euler(0f, currentYawOffset, 0f);

                // Basic placement point
                pos = surfacePoint + surfaceNormal * held.surfaceOffset;
                pos += (rot * Vector3.up) * extraHeightOffset;

                // Snap along wall plane
                pos = SnapToGridOnPlane(pos, surfacePoint, surfaceNormal, gridSize, held.surfaceOffset);
            }
            else
            {
                // No wall -> preview invalid
                pos = ray.origin + ray.direction * 2f;
                rot = Quaternion.LookRotation(-ray.direction, Vector3.up);
            }
        }
        else // Floor
        {
            if (Physics.Raycast(ray, out var hit, maxPlaceDistance, floorMask, QueryTriggerInteraction.Ignore))
            {
                hasSurfaceHit = true;
                surfacePoint = hit.point;
                surfaceNormal = hit.normal;

                // Stable world yaw + rotate steps ONLY (no camera-forward alignment)
                rot = Quaternion.Euler(0f, baseYawWorld + currentYawOffset, 0f);

                // Align bottom of object to floor based on ghost bounds
                var origin = (ghostPlaceable && ghostPlaceable.snapOrigin) ? ghostPlaceable.snapOrigin : ghost.transform;
                float bottomToOrigin = GetBottomOffsetFromGhost(origin);

                pos = surfacePoint + Vector3.up * (held.surfaceOffset + bottomToOrigin + extraHeightOffset);

                // Grid snap on XZ (world grid)
                pos = new Vector3(
                    Mathf.Round(pos.x / gridSize) * gridSize,
                    pos.y,
                    Mathf.Round(pos.z / gridSize) * gridSize
                );
            }
            else
            {
                // No floor -> preview invalid
                pos = ray.origin + ray.direction * 2f;
                rot = Quaternion.Euler(0f, baseYawWorld + currentYawOffset, 0f);
            }
        }

        ghost.transform.SetPositionAndRotation(pos, rot);

        // Validity + visuals
        bool valid = IsCurrentGhostPlacementValid();
        ApplyPreviewTint(valid ? validTint : invalidTint);

        // Footprint update
        UpdateFootprint(valid);
    }

    private float GetBottomOffsetFromGhost(Transform origin)
    {
        // distance from origin.y to the lowest renderer bound (works even with colliders disabled)
        if (ghostRenderers.Count == 0) return 0f;

        float minY = float.PositiveInfinity;
        for (int i = 0; i < ghostRenderers.Count; i++)
            minY = Mathf.Min(minY, ghostRenderers[i].bounds.min.y);

        return origin.position.y - minY;
    }

    private Vector3 SnapToGridOnPlane(Vector3 point, Vector3 planePoint, Vector3 planeNormal, float step, float surfaceOffset)
    {
        var right = Vector3.Cross(Vector3.up, planeNormal).normalized;
        if (right.sqrMagnitude < 1e-6f) right = Vector3.right;

        var upWall = Vector3.Cross(planeNormal, right).normalized;

        var local = new Vector2(
            Vector3.Dot(point - planePoint, right),
            Vector3.Dot(point - planePoint, upWall)
        );

        local = new Vector2(
            Mathf.Round(local.x / step) * step,
            Mathf.Round(local.y / step) * step
        );

        return planePoint + right * local.x + upWall * local.y + planeNormal * surfaceOffset;
    }

    private bool TryCommitGhost()
    {
        if (!ghost || !held) return false;
        if (!IsCurrentGhostPlacementValid()) return false;

        held.transform.SetPositionAndRotation(ghost.transform.position, ghost.transform.rotation);
        held.gameObject.SetActive(true);
        held.RecalcBounds();
        held.NotifyPlacedOrMoved();
        return true;
    }

    private bool IsCurrentGhostPlacementValid()
    {
        // Must be on a valid surface raycast
        if (!hasSurfaceHit) return false;

        // Must NOT overlap blocking layers
        GetGhostOBB(out var center, out var halfExtents);

        int count = Physics.OverlapBoxNonAlloc(
             center,
             halfExtents * overlapPadding,
             _overlaps,
             ghost.transform.rotation,
             blockMask,
             triggerInteraction
         );

        // If buffer fills up, treat as blocked (safer than accidentally allowing overlap)
        if (count >= _overlaps.Length) return false;


        for (int i = 0; i < count; i++)
        {
            var c = _overlaps[i];
            if (!c) continue;

            // Ignore footprint (no collider) and ghost (no colliders), but keep this guard anyway
            if (ghost && c.transform.IsChildOf(ghost.transform)) continue;

            return false;
        }

        return true;
    }
    private void GetGhostOBB(out Vector3 centerWorld, out Vector3 halfExtentsWorld)
    {
        Bounds local = _hasHeldLocalBounds ? _heldLocalBounds : GetGhostLocalBounds();

        centerWorld = ghost.transform.TransformPoint(local.center);

        var s = ghost.transform.lossyScale;
        s = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        halfExtentsWorld = Vector3.Scale(local.extents, s);
    }

    private Bounds GetGhostLocalBounds()
    {
        if (!ghost) return new Bounds(Vector3.zero, Vector3.one * 0.5f);

        var rends = ghostRenderers.Count > 0 ? ghostRenderers : new List<Renderer>(ghost.GetComponentsInChildren<Renderer>(true));
        if (rends.Count == 0) return new Bounds(Vector3.zero, Vector3.one * 0.5f);

        var w2g = ghost.transform.worldToLocalMatrix;
        bool init = false;
        Bounds b = default;

        for (int i = 0; i < rends.Count; i++)
        {
            var r = rends[i];
            if (!r) continue;

            var lb = r.localBounds;
            var l2w = r.localToWorldMatrix;

            // 8 corners of local bounds
            Vector3 c = lb.center;
            Vector3 e = lb.extents;

            for (int xi = -1; xi <= 1; xi += 2)
                for (int yi = -1; yi <= 1; yi += 2)
                    for (int zi = -1; zi <= 1; zi += 2)
                    {
                        Vector3 cornerLocal = c + Vector3.Scale(e, new Vector3(xi, yi, zi));
                        Vector3 cornerWorld = l2w.MultiplyPoint3x4(cornerLocal);
                        Vector3 cornerGhostLocal = w2g.MultiplyPoint3x4(cornerWorld);

                        if (!init)
                        {
                            b = new Bounds(cornerGhostLocal, Vector3.zero);
                            init = true;
                        }
                        else b.Encapsulate(cornerGhostLocal);
                    }
        }

        return init ? b : new Bounds(Vector3.zero, Vector3.one * 0.5f);
    }

    private void ApplyPreviewTint(Color tint)
    {
        // Ghost tint
        for (int i = 0; i < ghostRenderers.Count; i++)
            SetRendererTint(ghostRenderers[i], tint);

        // Footprint tint
        if (footprintRenderer)
            SetRendererTint(footprintRenderer, tint);
    }

    private void SetRendererTint(Renderer r, Color tint)
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        if (!r) return;

        r.GetPropertyBlock(_mpb);

        // Support common pipelines
        var mat = r.sharedMaterial;
        if (mat)
        {
            if (mat.HasProperty(_BaseColorID)) _mpb.SetColor(_BaseColorID, tint);
            else if (mat.HasProperty(_ColorID)) _mpb.SetColor(_ColorID, tint);
            else if (mat.HasProperty(_TintColorID)) _mpb.SetColor(_TintColorID, tint);
        }

        r.SetPropertyBlock(_mpb);
    }

    private void UpdateFootprint(bool valid)
    {
        if (!footprint || !footprintRenderer) return;

        // Show only when we have an actual surface hit
        footprint.SetActive(hasSurfaceHit);
        if (!hasSurfaceHit) return;

        // Use the same OBB half extents we use for collision
        GetGhostOBB(out _, out var halfExtentsWorld);

        // Place slightly off the surface to avoid z-fighting
        Vector3 p = surfacePoint + surfaceNormal * (held.surfaceOffset + footprintSurfaceNudge);

        // Footprint orientation + size depends on surface type:
        // - Floor: width = X, height = Z, rotate with ghost yaw
        // - Wall:  width = X, height = Y, orient to wall plane
        float width, height;
        Quaternion r;

        if (held.placementType == Placeable.PlacementType.Wall)
        {
            width = halfExtentsWorld.x * 2f;
            height = halfExtentsWorld.y * 2f;

            // Quad forward should face out of wall (surfaceNormal), with "up" matching ghost up
            r = Quaternion.LookRotation(surfaceNormal, ghost.transform.up);
        }
        else
        {
            width = halfExtentsWorld.x * 2f;
            height = halfExtentsWorld.z * 2f;

            // Quad forward should face up (surfaceNormal), and its up axis aligns with ghost forward (so it rotates with yaw)
            Vector3 upInPlane = Vector3.ProjectOnPlane(ghost.transform.forward, surfaceNormal).normalized;
            if (upInPlane.sqrMagnitude < 1e-6f) upInPlane = Vector3.forward;
            r = Quaternion.LookRotation(surfaceNormal, upInPlane);
        }

        footprint.transform.SetPositionAndRotation(p, r);
        footprint.transform.localScale = new Vector3(width, height, 1f);
    }
}
