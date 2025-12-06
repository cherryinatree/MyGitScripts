using UnityEngine;

namespace Cherry.Anomalies
{
    public class LightFlickerAnomaly : AnomalyBase
    {
        [SerializeField] private Light[] lights;
        [SerializeField] private float flickerSpeed = 12f;
        [SerializeField] private float minIntensityMul = 0.2f;

        private float[] originalIntensities;

        public bool lightsOff = false;

        protected override void OnEnable()
        {
            Type = AnomalyType.Lights;
            base.OnEnable();
        }

        private void Awake()
        {
            Type = AnomalyType.Lights;
        }

        protected override void Activate_Internal()
        {
            if (lights == null || lights.Length == 0)
                lights = GetComponentsInChildren<Light>(true);

            originalIntensities = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++)
                originalIntensities[i] = lights[i].intensity;
        }

        protected override void Deactivate_Internal()
        {
            if (lights == null || originalIntensities == null) return;
            for (int i = 0; i < lights.Length; i++)
                lights[i].intensity = originalIntensities[i];
        }

        protected override bool CheckResolved_Internal() => false;

        private void LateUpdate()
        {
            if (State != AnomalyState.Active || lights == null) return;
            if (lightsOff)
            {
                for (int i = 0; i < lights.Length; i++)
                {
                    lights[i].intensity = 0f;
                }
                return;
            }
            float t = Time.time * flickerSpeed;
            for (int i = 0; i < lights.Length; i++)
            {
                float noise = Mathf.PerlinNoise(t, i * 13.7f);
                lights[i].intensity = originalIntensities[i] * Mathf.Lerp(minIntensityMul, 1f, noise);
            }
        }
    }
}
