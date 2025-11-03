using UnityEngine;
using UnityEngine.InputSystem; // New Input System
using System.Collections.Generic;

public class PlacementAction : PlayerAction
{
    [Header("Refs")]
    [SerializeField] private Camera viewCam;
    [SerializeField] private LayerMask floorMask;
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private LayerMask blockMask; // other placeables/environment for collision checks

    [Header("Preview")]
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private float maxPlaceDistance = 8f;
    [SerializeField] private float gridSize = 0.25f;
    [SerializeField] private float rotateStep = 15f;
    [SerializeField] private float raiseStep = 0.1f;

    private Placeable held;
    private GameObject ghost;
    private List<Renderer> ghostRenderers = new();

    private float extraHeightOffset;
    private float currentYawOffset;

    protected override void Subscribe(CorePlayer c) { /* no-op; using direct input */ }
    protected override void Unsubscribe(CorePlayer c) { /* no-op */ }

    void Reset() { if (!viewCam) viewCam = Camera.main; }

    void Update()
    {
        if (!viewCam) return;

        // Pick Up (Left Click on placeable)
        if (Mouse.current.leftButton.wasPressedThisFrame && held == null)
        {
            if (TryRaycastPlaceable(out var p))
                StartHolding(p);
        }

        // Holding -> Update ghost & Place/Cancel
        if (held != null)
        {
            UpdateGhostPose();

            if (Keyboard.current.qKey.wasPressedThisFrame) currentYawOffset -= rotateStep;
            if (Keyboard.current.eKey.wasPressedThisFrame) currentYawOffset += rotateStep;
            //if (Keyboard.current.rKey.wasPressedThisFrame) extraHeightOffset += raiseStep;
            //if (Keyboard.current.fKey.wasPressedThisFrame) extraHeightOffset -= raiseStep;

            // Confirm place
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
               // if (TryCommitGhost())
                  //  StopHolding();
            }

            // Cancel (put it back where it was)
            if (Mouse.current.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (TryCommitGhost())
                    StopHolding();
               // CancelHolding();
            }
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
    bool TryRaycastPlaceable(out Placeable placeable)
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

    void StartHolding(Placeable p, bool spawnedNow = false)
    {
        held = p;
        currentYawOffset = 0f;
        extraHeightOffset = 0f;

        // Create ghost
        ghost = Instantiate(held.gameObject);
        ghost.name = held.name + " (Ghost)";
        foreach (var c in ghost.GetComponentsInChildren<Collider>()) c.enabled = false;
        ghostRenderers.Clear();
        foreach (var r in ghost.GetComponentsInChildren<Renderer>())
        {
            ghostRenderers.Add(r);
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = ghostMaterial;
            r.sharedMaterials = mats;
        }

        // Hide the real one while holding (or keep it in hand)
        held.gameObject.SetActive(false);

        // If it blocks navmesh, carving waits until placed (since colliders off on ghost)
    }

    void StopHolding()
    {
        if (ghost) Destroy(ghost);
        held = null;
    }

    void CancelHolding()
    {
        // Restore original at its original transform (still stored on held)
        if (held)
        {
            held.gameObject.SetActive(true);
            held.NotifyPlacedOrMoved();
        }
        StopHolding();
    }

    void UpdateGhostPose()
    {
        if (!ghost || !held) return;
        Debug.Log("Updating ghost pose");
        Vector3 pos;
        Quaternion rot;

        var kind = held.placementType;
        var ray = viewCam.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        if (kind == Placeable.PlacementType.Wall)
        {
            if (Physics.Raycast(ray, out var hit, maxPlaceDistance, wallMask))
            {
                var normal = hit.normal;
                rot = Quaternion.LookRotation(-normal, Vector3.up) * Quaternion.Euler(0f, currentYawOffset, 0f);
                pos = hit.point + normal * held.surfaceOffset;

                pos += ghost.transform.up * extraHeightOffset;

                // Optional grid along wall plane: snap the tangent axes
                pos = SnapToGridOnPlane(pos, hit.point, normal, gridSize);
            }
            else
            {
                // no wall under crosshair: float in front of camera for feedback
                pos = ray.origin + ray.direction * 2f;
                rot = Quaternion.LookRotation(-ray.direction, Vector3.up);
            }
        }
        else // Floor
        {
            if (Physics.Raycast(ray, out var hit, maxPlaceDistance, floorMask))
            {
                var basePos = hit.point;

                // Align bottom of object to floor
                var origin = held.snapOrigin ? held.snapOrigin : held.transform;
                var bottomToOrigin = GetBottomOffset(held, origin);

                pos = basePos + Vector3.up * (held.surfaceOffset + bottomToOrigin + extraHeightOffset);

                // Yaw follows camera forward (flat) + rotate steps
                var flatFwd = Vector3.ProjectOnPlane(viewCam.transform.forward, Vector3.up);
                if (flatFwd.sqrMagnitude < 1e-4f) flatFwd = Vector3.forward;
                rot = Quaternion.LookRotation(flatFwd, Vector3.up) * Quaternion.Euler(0f, currentYawOffset, 0f);

                // Grid snap on XZ
                pos = new Vector3(
                    Mathf.Round(pos.x / gridSize) * gridSize,
                    pos.y,
                    Mathf.Round(pos.z / gridSize) * gridSize
                );
            }
            else
            {
                pos = ray.origin + ray.direction * 2f;
                rot = Quaternion.LookRotation(Vector3.ProjectOnPlane(viewCam.transform.forward, Vector3.up), Vector3.up);
            }
        }

        ghost.transform.SetPositionAndRotation(pos, rot);
    }

    float GetBottomOffset(Placeable p, Transform origin)
    {
        // distance from origin.y to the lowest collider bound
        var cols = p.GetComponentsInChildren<Collider>();
        if (cols.Length == 0) return 0f;
        float minY = float.PositiveInfinity;
        foreach (var c in cols) minY = Mathf.Min(minY, c.bounds.min.y);
        return origin.position.y - minY;
    }

    Vector3 SnapToGridOnPlane(Vector3 point, Vector3 planePoint, Vector3 planeNormal, float step)
    {
        var right = Vector3.Cross(Vector3.up, planeNormal).normalized;
        var upWall = Vector3.Cross(planeNormal, right).normalized;

        var local = new Vector2(
            Vector3.Dot(point - planePoint, right),
            Vector3.Dot(point - planePoint, upWall)
        );

        local = new Vector2(
            Mathf.Round(local.x / step) * step,
            Mathf.Round(local.y / step) * step
        );

        return planePoint + right * local.x + upWall * local.y + planeNormal * held.surfaceOffset;
    }

    bool TryCommitGhost()
    {
        if (!ghost || !held) return false;

        // Collision check using ghost render bounds
        var bounds = GetGhostWorldBounds();
        var hits = Physics.OverlapBox(bounds.center, bounds.extents * 0.98f, ghost.transform.rotation, blockMask, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            // ignore if it’s a child of the ghost itself (shouldn’t happen since ghost colliders are off)
            if (h.transform.IsChildOf(ghost.transform)) continue;
            return false; // blocked
        }

        // Apply pose to held, reactivate and notify
        held.transform.SetPositionAndRotation(ghost.transform.position, ghost.transform.rotation);
        held.gameObject.SetActive(true);
        held.RecalcBounds();
        held.NotifyPlacedOrMoved();
        return true;
    }

    Bounds GetGhostWorldBounds()
    {
        var rends = ghost.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return new Bounds(ghost.transform.position, Vector3.one * 0.5f);
        var b = new Bounds(rends[0].bounds.center, rends[0].bounds.size);
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b;
    }
}
