using UnityEngine;

public class TriggerAudioSource : MonoBehaviour
{

    public AudioSource[] audioSource;
    public bool audioCutOff = false;
    public float cutOffTime = 3f;
    private Timer cutOffTimer;

    private void Start()
    {
        if (audioCutOff)
        {
            cutOffTimer = new Timer(cutOffTime);
        }
    }

    private void Update()
    {
        if (audioCutOff && cutOffTimer != null)
        {
            if (cutOffTimer.ClockTick())
            {
                foreach (AudioSource source in audioSource)
                {
                    if (source != null && source.isPlaying)
                    {
                        source.Stop();
                    }
                }
            }
        }
    }

    public void PlayAudio()
    {
        foreach (AudioSource source in audioSource)
        {
            if (source != null)
            {
                source.Play();
            }
        }
    }
}
