using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Cherry.ArcadeAI
{
    /// <summary>
    /// Handles how many customers decide to show up today and when they enter.
    /// Hook StartArcadeDay to your "open arcade" button/event.
    /// Hook EndArcadeDay to your "close arcade" button/event.
    /// </summary>
    public class ArcadeTrafficManager : MonoBehaviour
    {
        public static ArcadeTrafficManager Instance { get; private set; }

        [Header("Customer Prefabs")]
        public List<GameObject> customerPrefabs = new List<GameObject>();

        [Header("Spawn Points")]
        public List<Transform> spawnPoints = new List<Transform>();

        [Header("Spawn Timing")]
        public Vector2 spawnIntervalRange = new Vector2(4f, 10f);
        public bool spawnOnlyWhenBelowCapacity = true;

        [Header("Daily Count")]
        public int customersSpawnedToday;
        public int targetVisitorsToday;

        [Header("Runtime")]
        public bool arcadeOpen;

        [Header("Events")]
        public UnityEvent onArcadeDayStarted;
        public UnityEvent onArcadeDayEnded;
        public UnityEvent onCustomerSpawned;

        private Coroutine spawnRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void StartArcadeDay()
        {
            customersSpawnedToday = 0;

            int maxCapacity = ArcadeCapacityManager.Instance != null
                ? ArcadeCapacityManager.Instance.MaxCustomersAllowed
                : 10;

            targetVisitorsToday = ArcadeReputationManager.Instance != null
                ? ArcadeReputationManager.Instance.GetTargetVisitorsToday(maxCapacity)
                : maxCapacity;

            arcadeOpen = true;

            if (spawnRoutine != null)
                StopCoroutine(spawnRoutine);

            spawnRoutine = StartCoroutine(SpawnRoutine());
            onArcadeDayStarted?.Invoke();
        }

        public void EndArcadeDay()
        {
            arcadeOpen = false;

            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }

            onArcadeDayEnded?.Invoke();
        }

        public void ForceSpawnCustomer()
        {
            TrySpawnCustomer(true);
        }

        private IEnumerator SpawnRoutine()
        {
            while (arcadeOpen)
            {
                TrySpawnCustomer(false);

                float min = Mathf.Min(spawnIntervalRange.x, spawnIntervalRange.y);
                float max = Mathf.Max(spawnIntervalRange.x, spawnIntervalRange.y);
                float wait = Random.Range(Mathf.Max(0.1f, min), Mathf.Max(0.1f, max));

                yield return new WaitForSeconds(wait);
            }
        }

        private void TrySpawnCustomer(bool ignoreDailyLimit)
        {
            if (customerPrefabs.Count == 0 || spawnPoints.Count == 0)
                return;

            if (!ignoreDailyLimit && customersSpawnedToday >= targetVisitorsToday)
                return;

            if (spawnOnlyWhenBelowCapacity && ArcadeCapacityManager.Instance != null)
            {
                if (!ArcadeCapacityManager.Instance.CanCustomerEnter())
                    return;
            }

            GameObject prefab = customerPrefabs[Random.Range(0, customerPrefabs.Count)];
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];

            GameObject customerObject = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

            customersSpawnedToday++;

            CustomerBrain customerBrain = customerObject.GetComponent<CustomerBrain>();
            if (customerBrain != null)
                customerBrain.wasCountedByCapacityManager = true;

            if (ArcadeCapacityManager.Instance != null)
                ArcadeCapacityManager.Instance.RegisterCustomerEntered();

            onCustomerSpawned?.Invoke();
        }
    }
}
