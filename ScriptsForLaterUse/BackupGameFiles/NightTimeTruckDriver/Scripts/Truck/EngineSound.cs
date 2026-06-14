using UnityEngine;


public class EngineSound : MonoBehaviour
{
    public AudioSource engineAudio;
    public Rigidbody truckRb;
    public float minPitch = 0.8f;
    public float maxPitch = 2.0f;

    void Update()
    {
        float speed = truckRb.linearVelocity.magnitude;
        engineAudio.pitch = Mathf.Lerp(minPitch, maxPitch, speed / 100f);
    }
}