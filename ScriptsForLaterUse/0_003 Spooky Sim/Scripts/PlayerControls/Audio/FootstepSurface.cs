using UnityEngine;

public class FootstepSurface : MonoBehaviour
{
    [Tooltip("Clips to use when stepping on this collider.")]
    public AudioClip[] footstepClips;
    [Range(0.2f, 2f)]
    public float volumeScale = 1f;
}
