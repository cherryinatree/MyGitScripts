using UnityEngine;
using UnityEngine.Rendering;

public class TimeOfDaySwitcher : MonoBehaviour
{
    public Volume dayVolume;
    public Volume nightVolume;
    public Light sunLight;
    public Light moonLight;

    public void SetNight()
    {
        dayVolume.enabled = false;
        nightVolume.enabled = true;
        sunLight.enabled = false;
        moonLight.enabled = true;
    }

    public void SetDay()
    {
        dayVolume.enabled = true;
        nightVolume.enabled = false;
        sunLight.enabled = true;
        moonLight.enabled = false;
    }
}
