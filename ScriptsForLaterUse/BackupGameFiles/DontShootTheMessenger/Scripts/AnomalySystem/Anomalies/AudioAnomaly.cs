using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioAnomaly : Anomaly
{
    public AudioClip clip;

    public override void Activate(GameObject player)
    {
        Debug.Log($"Audio anomaly triggered: {anomalyName}");
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }
}