using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Gameplay/Beam Interaction Router")]
public class BeamInteractionRouter : MonoBehaviour
{
    [Tooltip("Optional. If present, we use it for harvesting (recommended).")]
    [SerializeField] private BeamHitRouter harvestRouter;

    // Backward-compatible (no path)
    public void HandleHit(RaycastHit hit, BeamRayDefinition ray, float dt, float energy, Transform beamOrigin)
        => HandleHit(hit, ray, dt, energy, beamOrigin, null);

    // NEW: path-aware
    public void HandleHit(
        RaycastHit hit,
        BeamRayDefinition ray,
        float dt,
        float energy,
        Transform beamOrigin,
        IReadOnlyList<Vector3> returnPath)
    {
        // 1) Damage enemies (only if this ray has DPS)
        if (ray != null && ray.damagePerSecond > 0f)
        {
            var enemy = hit.collider.GetComponentInParent<BeamEnemy>();
            if (enemy != null && !enemy.IsDead && enemy.CanBeDamagedBy(ray))
            {
                float dmg = ray.damagePerSecond * Mathf.Max(0f, dt) * Mathf.Clamp01(energy);
                enemy.ApplyBeamDamage(dmg, ray, hit.point, beamOrigin);
            }
        }

        // 2) Harvest (only if ray can harvest)
        if (ray != null && !ray.canHarvest) return;

        if (harvestRouter == null)
            harvestRouter = GetComponent<BeamHitRouter>();

        if (harvestRouter != null)
            harvestRouter.HandleHit(hit, returnPath);
    }
}