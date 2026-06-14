using System;
using UnityEngine;
[Flags]
public enum SurfaceCategory
{
    None = 0,
    Top = 1 << 0, // upward facing
    Side = 1 << 1, // walls / vertical-ish
    Bottom = 1 << 2, // downward facing (undersides)
    Any = Top | Side | Bottom
}
public class CollectibleAnchor : MonoBehaviour
{
    [Header("Attachment")]
    [Tooltip("Local-space direction that represents the 'bottom' of this collectible.")]
    public Vector3 bottomLocalDirection = Vector3.down;

    [Tooltip("Local-space offset from pivot to the contact point on the bottom.")]
    public Vector3 pivotToBottomOffset = Vector3.zero;

    [Header("Overlap / Clearance")]
    [Tooltip("Half-extents for overlap checks, in local space units.")]
    public Vector3 clearanceHalfExtents = new Vector3(0.1f, 0.1f, 0.1f);

    [Tooltip("If true, this overrides the SO extents.")]
    public bool overridesClearanceExtents = false;

    public Vector3 BottomDirWorld(Transform t)
        => t.TransformDirection(bottomLocalDirection.normalized);

    public Vector3 ClearanceHalfExtentsWorld(Transform t)
    {
        // approximate world extents by lossy scale
        Vector3 s = t.lossyScale;
        return new Vector3(
            clearanceHalfExtents.x * Mathf.Abs(s.x),
            clearanceHalfExtents.y * Mathf.Abs(s.y),
            clearanceHalfExtents.z * Mathf.Abs(s.z)
        );
    }
}
