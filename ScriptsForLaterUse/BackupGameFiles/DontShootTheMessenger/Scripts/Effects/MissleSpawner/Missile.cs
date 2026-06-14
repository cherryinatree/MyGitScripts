using UnityEngine;

public class Missile : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private GameObject explosionPrefab;

    public float travelTime = 4.5f; // how long it takes to reach target
    private float timer = 0f;

    public float arcHeight = 150f; // controls the curve height
    private bool initialized = false;

    public void Initialize(Vector3 target, GameObject explosion)
    {
        startPosition = transform.position;
        targetPosition = target;
        explosionPrefab = explosion;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;

        timer += Time.deltaTime;
        float t = timer / travelTime;

        if (t >= 1f)
        {
            Explode();
            return;
        }

        // compute current position with arc
        Vector3 nextPos = Vector3.Lerp(startPosition, targetPosition, t);
        float height = Mathf.Sin(t * Mathf.PI) * arcHeight;
        nextPos.y += height;

        // find direction from current to next
        Vector3 moveDir = nextPos - transform.position;
        if (moveDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDir);
        }

        // finally update position
        transform.position = nextPos;
    }

    private void Explode()
    {
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, targetPosition, Quaternion.identity);
        }

        Destroy(gameObject); // destroy missile
    }
}
