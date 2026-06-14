using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class VisualEffectsManager : MonoBehaviour
{
    public static VisualEffectsManager Instance;

    [Header("Post Processing Volume")]
    public Volume postProcessVolume;
    private LensDistortion lensDistortion;
    private ChromaticAberration chromaticAberration;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    void Start()
    {
        if (postProcessVolume.profile.TryGet(out lensDistortion))
        {
            lensDistortion.active = true;
            lensDistortion.intensity.value = 0f;
        }
        if (postProcessVolume.profile.TryGet(out chromaticAberration))
        {
            chromaticAberration.active = true;
            chromaticAberration.intensity.value = 0f;
        }
    }

    public void SetDistortion(float intensity)
    {
        if (lensDistortion != null)
            lensDistortion.intensity.value = intensity;
    }

    public void SetChromaticAberration(float intensity)
    {
        if (chromaticAberration != null)
            chromaticAberration.intensity.value = intensity;
    }
}
