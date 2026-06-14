using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FearZone : MonoBehaviour
{
    [SerializeField] private bool distortAudio = true;
    [SerializeField] private bool distortVisuals = true;



    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (distortAudio) AudioManager.Instance.StartDistortion();
            if (distortVisuals)
            {
                VisualEffectsManager.Instance.SetDistortion(0.6f);
                VisualEffectsManager.Instance.SetChromaticAberration(0.5f);
            }

                // Optional: Play whisper for atmosphere
                AudioManager.Instance.PlayWhisper();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (distortAudio) AudioManager.Instance.StopDistortion();
            if (distortVisuals)
            {
                VisualEffectsManager.Instance.SetDistortion(0f);
                VisualEffectsManager.Instance.SetChromaticAberration(0f);
            }
        }
    }
}
