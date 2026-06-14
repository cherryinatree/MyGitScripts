using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Gameplay/Beam Optic Surface")]
[DisallowMultipleComponent]
public class BeamOpticSurface : MonoBehaviour
{
    public enum OpticsMode { Mirror, GlassMirror, Prism }
    public enum ConversionMode { None, ForceRay }

    [Header("Optics")]
    public OpticsMode opticsMode = OpticsMode.Mirror;

    [Range(0, 16)] public int prismSplitCount = 2;
    [Range(0f, 120f)] public float prismSpreadAngle = 25f;
    public Vector3 prismLocalFanAxis = Vector3.up;

    [Range(0f, 1f)] public float reflectionWeight = 1f;
    [Range(0f, 1f)] public float transmissionWeight = 1f;

    [Header("Ray Conversion (Base)")]
    public ConversionMode baseConversion = ConversionMode.None;
    public BeamRayDefinition forcedRay;

    [Header("Temporary Coating")]
    public bool allowCoating = true;
    public BeamRayDefinition coatingForcedRay;     // e.g. Ray_GhostGreen
    public float coatingDuration = 6f;

    private float coatingUntilTime = -1f;

    [Header("Tap Rotation")]
    public bool allowTapRotate = true;
    public Vector3 localRotationAxis = Vector3.up;
    public float stepDegrees = 15f;

    public struct OutBeam
    {
        public Vector3 dir;
        public float weight;
        public BeamRayDefinition ray;
    }

    public void ApplyCoatingNow()
    {
        if (!allowCoating || coatingForcedRay == null) return;
        coatingUntilTime = Time.time + Mathf.Max(0.05f, coatingDuration);
    }

    public bool IsCoated => coatingUntilTime > Time.time;

    public void RotateStep(int direction = 1)
    {
        if (!allowTapRotate) return;
        float degrees = stepDegrees * Mathf.Sign(direction == 0 ? 1 : direction);
        transform.Rotate(localRotationAxis.normalized, degrees, Space.Self);
    }

    public bool GetOutgoingBeams(
        BeamRayDefinition incomingRay,
        Vector3 incomingDir,
        Vector3 hitNormal,
        List<OutBeam> outBeams)
    {
        outBeams.Clear();

        if (incomingRay != null && !incomingRay.canBeReflected && opticsMode == OpticsMode.Mirror)
            return false;

        BeamRayDefinition outRay = ResolveOutputRay(incomingRay);

        incomingDir = incomingDir.normalized;
        hitNormal = hitNormal.normalized;

        if (opticsMode == OpticsMode.Mirror)
        {
            outBeams.Add(new OutBeam
            {
                dir = Vector3.Reflect(incomingDir, hitNormal).normalized,
                weight = Mathf.Clamp01(reflectionWeight <= 0f ? 1f : reflectionWeight),
                ray = outRay
            });
            return true;
        }

        if (opticsMode == OpticsMode.GlassMirror)
        {
            if (reflectionWeight > 0f)
            {
                outBeams.Add(new OutBeam
                {
                    dir = Vector3.Reflect(incomingDir, hitNormal).normalized,
                    weight = Mathf.Clamp01(reflectionWeight),
                    ray = outRay
                });
            }

            if (transmissionWeight > 0f)
            {
                outBeams.Add(new OutBeam
                {
                    dir = incomingDir,
                    weight = Mathf.Clamp01(transmissionWeight),
                    ray = outRay
                });
            }

            return outBeams.Count > 0;
        }

        // Prism: split around an axis
        int count = Mathf.Clamp(prismSplitCount, 2, 16);
        float total = Mathf.Clamp(prismSpreadAngle, 0f, 120f);
        float half = total * 0.5f;

        Vector3 axisWorld = transform.TransformDirection(prismLocalFanAxis.normalized);
        if (axisWorld.sqrMagnitude < 0.0001f) axisWorld = Vector3.up;

        float w = 1f / count;
        for (int i = 0; i < count; i++)
        {
            float t = (count == 1) ? 0.5f : (i / (float)(count - 1));
            float ang = Mathf.Lerp(-half, half, t);
            Vector3 d = (Quaternion.AngleAxis(ang, axisWorld) * incomingDir).normalized;

            outBeams.Add(new OutBeam { dir = d, weight = w, ray = outRay });
        }

        return true;
    }

    private BeamRayDefinition ResolveOutputRay(BeamRayDefinition incoming)
    {
        // Coating overrides base conversion
        if (IsCoated && coatingForcedRay != null)
            return coatingForcedRay;

        if (baseConversion == ConversionMode.ForceRay && forcedRay != null)
            return forcedRay;

        return incoming;
    }
}