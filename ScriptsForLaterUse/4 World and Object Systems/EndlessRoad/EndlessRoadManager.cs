using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;



public class EndlessRoadManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] startSegments;      // Multiple possible starting roads
    public GameObject[] roadPrefabs;        // Normal road pieces
    public GameObject[] gasStationPrefabs;  // Gas stations
    public GameObject[] motelPrefabs;       // Motels to sleep at
    public GameObject[] spookyPrefabs;      // Roads with spooky events
    public GameObject[] endSegments;        // Destination/end roads

    [Header("Settings")]
    public float segmentLength = 50f;
    public int bufferForward = 6;
    public int bufferBackward = 3;
    public Transform startPosition;

    [Tooltip("Every N segments, spawn a gas station.")]
    public int gasStationInterval = 10;
    [Tooltip("Every N segments, spawn a motel.")]
    public int motelInterval = 20;
    [Tooltip("Chance (0-1) for a spooky road to appear.")]
    [Range(0f, 1f)] public float spookyChance = 0.15f;

    public int minEndSegmentIndex = 100;
    public int maxEndSegmentIndex = 200;

    private Dictionary<int, GameObject> roadSegments = new Dictionary<int, GameObject>();
    private int currentPlayerSegment = 0;
    private int endSegmentIndex;

    void Awake()
    {
        // Spawn truck + player at start
        GameObject truck = Instantiate(
            (GameObject)Resources.Load(SaveSingleton.Instance.truckStats.truckPath),
            new Vector3(startPosition.position.x, startPosition.position.y + 1, startPosition.position.z),
            Quaternion.identity
        );
        truck.name = "Truck";

        GameObject playerSpawn = truck.transform.Find("PlayerSpawnInTruck").gameObject;
        /*   GameObject playerModel = Instantiate(
              (GameObject)Resources.Load(SaveSingleton.Instance.truckStats.playerPath),
              playerSpawn.transform.position,
              Quaternion.identity
          );
          playerModel.transform.SetParent(truck.transform);
          playerModel.name = "PlayerModel1";*/

        GameObject playerWalkingModel = Instantiate(
            (GameObject)Resources.Load("Prefabs/Player/PlayerWalkingModel1"),
            playerSpawn.transform.position,
            Quaternion.identity
        );
        playerWalkingModel.transform.SetParent(truck.transform);
        playerWalkingModel.name = "PlayerWalkingModel1";
        //playerWalkingModel.SetActive(false);

        // Choose a random ending distance
        endSegmentIndex = Random.Range(minEndSegmentIndex, maxEndSegmentIndex + 1);
        UpdateRoads(forceFullRefresh: true);
    }

    void Update()
    {
        int playerSegment = Mathf.FloorToInt(startPosition.position.z / segmentLength);
        if (playerSegment != currentPlayerSegment)
        {
            currentPlayerSegment = playerSegment;
            UpdateRoads();
        }
    }

    void UpdateRoads(bool forceFullRefresh = false)
    {
        HashSet<int> targetSegments = new HashSet<int>();

        for (int i = currentPlayerSegment - bufferBackward; i <= currentPlayerSegment + bufferForward; i++)
        {
            targetSegments.Add(i);

            if (!roadSegments.ContainsKey(i))
            {
                GameObject prefabToUse = ChoosePrefabForSegment(i);
                Vector3 spawnPos = new Vector3(0, 0, i * segmentLength);

                GameObject segment = Instantiate(prefabToUse, spawnPos, Quaternion.identity);
                segment.name = $"Segment_{i}";

                var segmentScript = segment.GetComponent<RoadSegment>();
                if (segmentScript != null)
                    segmentScript.segmentIndex = i;

                roadSegments[i] = segment;
            }
        }

        // Remove segments too far away
        List<int> toRemove = new List<int>();
        foreach (var kvp in roadSegments)
        {
            if (!targetSegments.Contains(kvp.Key))
            {
                Destroy(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (int index in toRemove)
            roadSegments.Remove(index);
    }

    GameObject ChoosePrefabForSegment(int index)
    {
        if (index == 0) // Start
            return RandomFromArray(startSegments);

        if (index == endSegmentIndex) // Destination
            return RandomFromArray(endSegments);

        if (index > 0 && index % gasStationInterval == 0) // Gas Station
            return RandomFromArray(gasStationPrefabs);

        if (index > 0 && index % motelInterval == 0) // Motel
            return RandomFromArray(motelPrefabs);

        if (Random.value < spookyChance) // Spooky event road
            return RandomFromArray(spookyPrefabs);

        // Default road
        return RandomFromArray(roadPrefabs);
    }

    GameObject RandomFromArray(GameObject[] array)
    {
        if (array == null || array.Length == 0)
        {
            Debug.LogWarning("Prefab array is empty! Returning null.");
            return null;
        }
        return array[Random.Range(0, array.Length)];
    }

    public int GetEndSegmentIndex() => endSegmentIndex;
}



/*
public class EndlessRoadManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject startSegment;
    public GameObject[] roadPrefabs;
    public GameObject gasStationPrefab;
    public GameObject endSegmentPrefab;

    [Header("Settings")]
    public float segmentLength = 50f;
    public int bufferForward = 6;
    public int bufferBackward = 3;
    public Transform StartPosition;
    public int gasStationInterval = 10;
    public int minEndSegmentIndex = 100;
    public int maxEndSegmentIndex = 200;

    private Dictionary<int, GameObject> roadSegments = new Dictionary<int, GameObject>();
    private int currentPlayerSegment = 0;
    private int endSegmentIndex;

    void Awake()
    {
        GameObject truck = (GameObject)GameObject.Instantiate(Resources.Load(SaveSingleton.Instance.truckStats.truckPath),
            new Vector3(StartPosition.position.x, StartPosition.position.y + 1, StartPosition.position.z), Quaternion.identity);
        truck.name = "Truck";
        GameObject playerSpawn = truck.transform.Find("PlayerSpawnInTruck").gameObject;
        GameObject playerOutsideSpawn = truck.transform.Find("PlayerSpawnOutOfTruck").gameObject;

        GameObject playerModel = (GameObject)GameObject.Instantiate(Resources.Load(SaveSingleton.Instance.truckStats.playerPath), playerSpawn.transform.position, Quaternion.identity);
        playerModel.transform.SetParent(truck.transform);
        playerModel.name = "PlayerModel1";

        GameObject playerWalkingModel = (GameObject)GameObject.Instantiate(Resources.Load(
            "Prefabs/Player/PlayerWalkingModel1"), playerSpawn.transform.position, Quaternion.identity);
        playerWalkingModel.transform.SetParent(truck.transform);
        playerWalkingModel.name = "PlayerWalkingModel1";
        playerWalkingModel.SetActive(false);

        // Choose a random ending distance once
        endSegmentIndex = Random.Range(minEndSegmentIndex, maxEndSegmentIndex + 1);
        UpdateRoads(forceFullRefresh: true);
    }

    void Update()
    {
        int playerSegment = Mathf.FloorToInt(StartPosition.position.z / segmentLength);
        if (playerSegment != currentPlayerSegment)
        {
            currentPlayerSegment = playerSegment;
            UpdateRoads();
        }
    }

    void UpdateRoads(bool forceFullRefresh = false)
    {
        HashSet<int> targetSegments = new HashSet<int>();

        for (int i = currentPlayerSegment - bufferBackward; i <= currentPlayerSegment + bufferForward; i++)
        {
            targetSegments.Add(i);

            if (!roadSegments.ContainsKey(i))
            {
                GameObject prefabToUse;

                if (i == 0)
                {
                    prefabToUse = startSegment;
                }
                else if (i == endSegmentIndex)
                {
                    prefabToUse = endSegmentPrefab;
                }
                else if (i % gasStationInterval == 0 && i > 0)
                {
                    prefabToUse = gasStationPrefab;
                }
                else
                {
                    int prefabIndex = Mathf.Abs(i * 7919) % roadPrefabs.Length;
                    prefabToUse = roadPrefabs[prefabIndex];
                }

                Vector3 spawnPos = new Vector3(0, 0, i * segmentLength);
                GameObject segment = Instantiate(prefabToUse, spawnPos, Quaternion.identity);
                segment.name = $"Segment_{i}";

                var segmentScript = segment.GetComponent<RoadSegment>();
                if (segmentScript != null)
                {
                    segmentScript.segmentIndex = i;
                }

                roadSegments[i] = segment;
            }
        }

        // Remove segments too far away
        List<int> toRemove = new List<int>();
        foreach (var kvp in roadSegments)
        {
            if (!targetSegments.Contains(kvp.Key))
            {
                Destroy(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }

        foreach (int index in toRemove)
        {
            roadSegments.Remove(index);
        }
    }

    // Optional: Expose for UI or tracking
    public int GetEndSegmentIndex() => endSegmentIndex;
}
*/