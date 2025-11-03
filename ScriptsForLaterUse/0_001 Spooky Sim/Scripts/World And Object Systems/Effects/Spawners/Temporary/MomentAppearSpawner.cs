using UnityEngine;
using static AppearInFrontSpawner;

public class MomentAppearSpawner : MonoBehaviour
{


    public bool isOnTimer = true;
    public float lightOffTime = 5f;
    private float timeSinceStarted = 0f;

    public bool StartAfterDelay = false;
    public float delayTime = 1f;
    Timer spawnTimer;

    public Vector3 spawnOffsetPosition;

    private bool hasStarted = false;
    Timer timer;

    public GameObject Prefab;
    private GameObject spawn;

    private bool hasSpawned = false;

    public enum RotationMode { KeepPrefabRotation, MatchPlayerForward, LookAtPlayer }

    public RotationMode rotationMode = RotationMode.KeepPrefabRotation;

    // Update is called once per frame
    void Update()
    {
        if (hasStarted && isOnTimer)
        {
            if (StartAfterDelay)
            {
                Debug.Log("spawn");
                SpawnTimer();
            }
            else
            {
                DespawnTimer();
            }
        }
    }

    private void SpawnTimer()
    {
        if (spawnTimer.ClockTick())
        {
            if(hasSpawned == false)
            {

                SpawnObject();
                hasSpawned = true;
            }
            if (isOnTimer)
            {
                Debug.Log("Despawn1");
                DespawnTimer();
            }
        }
    }
    private void DespawnTimer()
    {
        if (timer.ClockTick())
        {
            Debug.Log("Despawn2");
            Destroy(spawn);
            hasStarted = false;

            hasSpawned = false;
            if (isOnTimer)
            {
                timer.RestartTimer();
            }
            if (StartAfterDelay)
            {
                spawnTimer.RestartTimer();
            }
        }
    }

    public void StartSpawn()
    {

        hasStarted = true;
        if (isOnTimer)
        {
            timer = new Timer(lightOffTime);
            timer.RestartTimer();
        }

        if(StartAfterDelay)
        {
            spawnTimer = new Timer(delayTime);
            spawnTimer.RestartTimer();
        }
        else
        {
           SpawnObject();
        }
    }

    private void SpawnObject()
    {
        spawn = Instantiate(Prefab, transform.position + spawnOffsetPosition, transform.rotation);


        // Rotation
        spawn.transform.rotation = rotationMode switch
        {
            RotationMode.KeepPrefabRotation => Quaternion.identity * transform.transform.rotation,
            RotationMode.MatchPlayerForward => Quaternion.LookRotation(transform.forward, Vector3.up),
            RotationMode.LookAtPlayer => Quaternion.LookRotation((transform.position - spawn.transform.position).normalized, Vector3.up),
            _ => Quaternion.identity
        };

    }
}
