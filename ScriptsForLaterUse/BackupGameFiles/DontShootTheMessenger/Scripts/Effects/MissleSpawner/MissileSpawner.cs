using UnityEngine;

public class MissileSpawner : MonoBehaviour
{
    [Header("Spawn & Target Points")]
    public Transform[] spawnPoints;   // where missiles start
    public Transform[] targetPoints;  // where missiles land

    [Header("Missile Settings")]
    public GameObject missilePrefab;
    public GameObject explosionPrefab;
    public float spawnInterval = 3f;

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnMissile();
            timer = 0f;
        }
    }

    private void SpawnMissile()
    {
        if (spawnPoints.Length == 0 || targetPoints.Length == 0 || missilePrefab == null) return;

        // pick random spawn and target
        Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Transform target = targetPoints[Random.Range(0, targetPoints.Length)];

        // spawn missile
        GameObject missile = Instantiate(missilePrefab, spawn.position, Quaternion.identity);

        // give missile its target
        Missile missileScript = missile.GetComponent<Missile>();
        if (missileScript != null)
        {
            missileScript.Initialize(target.position, explosionPrefab);
        }
    }
}
