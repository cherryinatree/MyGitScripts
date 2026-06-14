using UnityEngine;

public interface IBeamRayDamageable
{
    void ReceiveBeamRayHit(BeamRayDefinition rayDef, float damage, Vector3 hitPoint, Vector3 hitNormal);
}