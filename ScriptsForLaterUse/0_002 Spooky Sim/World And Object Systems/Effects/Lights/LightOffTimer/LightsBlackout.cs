using UnityEngine;


public class LightsBlackout : MonoBehaviour
{
   
    public bool isOnTimer = true;
    public float lightOffTime = 5f;
    private float timeSinceStarted = 0f;

    public GameObject[] lightsToTurnOff;

    private bool hasStarted = false;
    Timer timer;
    
    AudioSource _audioSource;
    public AudioClip lightOffSound;
    public AudioClip lightOnSound;

    // Update is called once per frame
    void Update()
    {
        if (hasStarted && isOnTimer)
        {
            LightsTimer();
        }
    }

    private void LightsTimer()
    {
        if (timer.ClockTick())
        {
            TurnOnLights();
        }
    }

    public void TurnOffLights()
    {
        foreach (GameObject lightObj in lightsToTurnOff)
        {
            lightObj.SetActive(false);

            if (lightOffSound != null && _audioSource != null)
                _audioSource.PlayOneShot(lightOffSound);
        }
        hasStarted =true;
       
        if(isOnTimer)
        {
            timer = new Timer(lightOffTime);
            timer.RestartTimer();
        }
    }

    public void TurnOnLights()
    {

        foreach (GameObject lightObj in lightsToTurnOff)
        {
            lightObj.SetActive(true);

            if (lightOnSound != null && _audioSource != null)
                _audioSource.PlayOneShot(lightOnSound);
        }
        hasStarted = false;

        if (isOnTimer)
        {
            timer.RestartTimer();
        }
    }
}
