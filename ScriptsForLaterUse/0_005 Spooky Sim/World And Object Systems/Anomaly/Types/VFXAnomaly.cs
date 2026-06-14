using UnityEngine;

namespace Cherry.Anomalies
{
    public class VFXAnomaly : AnomalyBase
    {
        [SerializeField] private ParticleSystem[] vfx;

        protected override void OnEnable()
        {
            Type = AnomalyType.VFX;
            base.OnEnable();
        }

        protected override void Activate_Internal()
        {
            if (vfx == null || vfx.Length == 0)
                vfx = GetComponentsInChildren<ParticleSystem>(true);
            Debug.Log($"Activating VFX Anomaly: {DisplayName} with {vfx.Length} particle systems.");
            foreach (var p in vfx)
            {
                p.Play(true);
                Debug.Log(p.isPlaying);
            }
        }

        protected override void Deactivate_Internal()
        {
            if (vfx == null) return;
            foreach (var p in vfx) p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        protected override bool CheckResolved_Internal()
        {
            // VFX anomalies are considered resolved immediately after activation
            return false;
        }
    }
}
