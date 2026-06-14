
using UnityEngine;

public class GhostTrigger : MonoBehaviour
{
    public GameObject ghostObject;
    public Transform ghostStartPoint;
    public Transform ghostEndPoint;
    public float ghostSpeed = 5f;
    public AudioClip ghostSound;

    private bool hasTriggered = false;
    private AudioSource audioSource;

    void Start()
    {
        if (ghostObject != null)
            ghostObject.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player")) // Tag your truck as "Player"
        {
            hasTriggered = true;
            StartCoroutine(PlayGhostEvent());
        }
    }

    System.Collections.IEnumerator PlayGhostEvent()
    {
        ghostObject.SetActive(true);
        ghostObject.transform.position = ghostStartPoint.position;

        if (ghostSound != null)
            audioSource.PlayOneShot(ghostSound);

        while (Vector3.Distance(ghostObject.transform.position, ghostEndPoint.position) > 0.1f)
        {
            ghostObject.transform.position = Vector3.MoveTowards(
                ghostObject.transform.position,
                ghostEndPoint.position,
                ghostSpeed * Time.deltaTime
            );
            yield return null;
        }

        ghostObject.SetActive(false);
    }
}