using UnityEngine;

namespace Cherry.Anomalies
{
    public class SoundAnomaly : AnomalyBase
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip clip;
        [SerializeField] private bool loop = true;

        protected override void OnEnable()
        {
            Type = AnomalyType.Sound;
            base.OnEnable();
        }


        protected override void Activate_Internal()
        {
            if (!source) source = GetComponent<AudioSource>();
            if (!source) return;

            source.clip = clip;
            source.loop = loop;
            source.Play();
        }

        protected override void Deactivate_Internal()
        {
            if (source) source.Stop();
        }

        // Example: never auto-resolves unless something else calls Resolve()
        protected override bool CheckResolved_Internal() => false;
    }
}
