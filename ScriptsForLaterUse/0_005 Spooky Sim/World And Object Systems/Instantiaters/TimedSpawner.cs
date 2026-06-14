using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Cherry.Spawning
{
    public enum SpawnPointPick
    {
        SingleOrSelf,      // Use Spawn Location if assigned, otherwise this transform
        RandomFromList     // Use a random entry from Extra Spawn Points (fallback to Spawn Location / self)
    }

    [DisallowMultipleComponent]
    public class TimedSpawner : MonoBehaviour
    {
        [Header("What to spawn")]
        [Tooltip("One or more prefabs to spawn.")]
        [SerializeField] private GameObject[] prefabs;

        [Tooltip("If true, chooses a random prefab each spawn. If false, uses Sequential.")]
        [SerializeField] private bool chooseRandomPrefab = true;

        [Tooltip("If not random, spawns prefabs in order.")]
        [SerializeField] private bool sequentialLoop = true;

        [Header("Where to spawn")]
        [Tooltip("Primary spawn location. If null, uses this transform.")]
        [SerializeField] private Transform spawnLocation;

        [Tooltip("Optional extra spawn points (for enemy spawners, drip arrays, etc.).")]
        [SerializeField] private Transform[] extraSpawnPoints;

        [SerializeField] private SpawnPointPick spawnPointPick = SpawnPointPick.SingleOrSelf;

        [Header("Timing")]
        [SerializeField] private bool autoStart = true;

        [SerializeField, Min(0f)] private float startDelay = 0f;

        [SerializeField, Min(0.01f)] private float interval = 1f;

        [Tooltip("Adds +- jitter to the interval.")]
        [SerializeField, Min(0f)] private float intervalJitter = 0f;

        [Tooltip("Spawn this many each tick.")]
        [SerializeField, Min(1)] private int burstCount = 1;

        [Tooltip("Use unscaled time (ignores Time.timeScale).")]
        [SerializeField] private bool useUnscaledTime = false;

        [Header("Limits")]
        [Tooltip("0 = unlimited. If > 0, spawner won't exceed this many alive at once.")]
        [SerializeField] private int maxAlive = 0;

        [Tooltip("0 = never auto-despawn. If > 0, spawned objects are returned/destroyed after this many seconds.")]
        [SerializeField] private float autoDespawnAfter = 0f;

        [Header("Spawn Transform Tweaks")]
        [Tooltip("Applied in spawn point local space.")]
        [SerializeField] private Vector3 localPositionOffset = Vector3.zero;

        [Tooltip("Random local offset within this box (each axis is +- value). Great for drip spread.")]
        [SerializeField] private Vector3 randomLocalPositionBox = Vector3.zero;

        [Tooltip("If true, spawned rotation starts from spawn point rotation.")]
        [SerializeField] private bool useSpawnRotation = true;

        [Tooltip("Rotation offset in degrees.")]
        [SerializeField] private Vector3 eulerOffset = Vector3.zero;

        [Tooltip("Random rotation in degrees within this box (each axis is +- value).")]
        [SerializeField] private Vector3 randomEulerBox = Vector3.zero;

        [Header("Hierarchy / Pooling")]
        [Tooltip("Optional parent for spawned objects.")]
        [SerializeField] private Transform spawnedParent;

        [Tooltip("If true, uses pooling per prefab to reduce Instantiate/Destroy churn.")]
        [SerializeField] private bool usePooling = false;

        [SerializeField, Min(1)] private int defaultPoolCapacity = 10;
        [SerializeField, Min(1)] private int maxPoolSize = 200;

        public bool IsRunning => _running;
        public int AliveCount
        {
            get { CleanupDeadAlive(); return _alive.Count; }
        }

        public event Action<GameObject> OnSpawned;

        private bool _running;
        private Coroutine _loopRoutine;

        private int _sequentialIndex;

        private readonly HashSet<GameObject> _alive = new();
        private readonly Dictionary<GameObject, Coroutine> _autoDespawnRoutines = new();

        private ObjectPool<GameObject>[] _pools;

        private void Awake()
        {
            _sequentialIndex = 0;

            if (usePooling)
                BuildPools();
        }

        private void OnEnable()
        {
            if (autoStart)
                StartSpawning();
        }

        private void OnDisable()
        {
            StopSpawning();
        }

        /// <summary>Start spawning on an interval.</summary>
        public void StartSpawning()
        {
            if (_running) return;
            if (prefabs == null || prefabs.Length == 0)
            {
                Debug.LogWarning($"{name}: TimedSpawner has no prefabs assigned.");
                return;
            }

            _running = true;
            _loopRoutine = StartCoroutine(SpawnLoop());
        }

        /// <summary>Stop spawning (does not despawn existing).</summary>
        public void StopSpawning()
        {
            _running = false;

            if (_loopRoutine != null)
            {
                StopCoroutine(_loopRoutine);
                _loopRoutine = null;
            }
        }

        /// <summary>Spawn immediately once (respects maxAlive).</summary>
        public GameObject SpawnOnce()
        {
            return SpawnInternal();
        }

        /// <summary>Spawn a full burst immediately (respects maxAlive).</summary>
        public void SpawnBurstNow()
        {
            for (int i = 0; i < burstCount; i++)
                SpawnInternal();
        }

        /// <summary>Despawn something that was spawned by this spawner. (Returns to pool if enabled.)</summary>
        public void Despawn(GameObject instance)
        {
            if (instance == null) return;

            if (_autoDespawnRoutines.TryGetValue(instance, out var routine) && routine != null)
            {
                StopCoroutine(routine);
                _autoDespawnRoutines.Remove(instance);
            }

            _alive.Remove(instance);

            if (usePooling && _pools != null)
            {
                var tag = instance.GetComponent<SpawnedByTimedSpawner>();
                if (tag != null && tag.Spawner == this && tag.PrefabIndex >= 0 && tag.PrefabIndex < _pools.Length)
                {
                    _pools[tag.PrefabIndex].Release(instance);
                    return;
                }
            }

            Destroy(instance);
        }

        private IEnumerator SpawnLoop()
        {
            if (startDelay > 0f)
            {
                if (useUnscaledTime) yield return new WaitForSecondsRealtime(startDelay);
                else yield return new WaitForSeconds(startDelay);
            }

            while (_running)
            {
                for (int i = 0; i < burstCount; i++)
                    SpawnInternal();

                float wait = interval;
                if (intervalJitter > 0f)
                    wait = Mathf.Max(0.01f, interval + UnityEngine.Random.Range(-intervalJitter, intervalJitter));

                if (useUnscaledTime) yield return new WaitForSecondsRealtime(wait);
                else yield return new WaitForSeconds(wait);
            }
        }

        private GameObject SpawnInternal()
        {
            CleanupDeadAlive();

            if (maxAlive > 0 && _alive.Count >= maxAlive)
                return null;

            int prefabIndex = PickPrefabIndex();
            if (prefabIndex < 0 || prefabIndex >= prefabs.Length || prefabs[prefabIndex] == null)
                return null;

            Transform point = PickSpawnPoint();

            Vector3 pos = point.position;
            Quaternion rot = useSpawnRotation ? point.rotation : Quaternion.identity;

            // Apply local offsets in the spawn point's space
            Vector3 localRand = new Vector3(
                UnityEngine.Random.Range(-randomLocalPositionBox.x, randomLocalPositionBox.x),
                UnityEngine.Random.Range(-randomLocalPositionBox.y, randomLocalPositionBox.y),
                UnityEngine.Random.Range(-randomLocalPositionBox.z, randomLocalPositionBox.z)
            );

            pos += point.TransformVector(localPositionOffset + localRand);

            Vector3 randEuler = new Vector3(
                UnityEngine.Random.Range(-randomEulerBox.x, randomEulerBox.x),
                UnityEngine.Random.Range(-randomEulerBox.y, randomEulerBox.y),
                UnityEngine.Random.Range(-randomEulerBox.z, randomEulerBox.z)
            );

            rot *= Quaternion.Euler(eulerOffset + randEuler);

            GameObject instance = GetInstance(prefabIndex);

            if (spawnedParent != null)
                instance.transform.SetParent(spawnedParent, worldPositionStays: true);
            else
                instance.transform.SetParent(null);

            instance.transform.SetPositionAndRotation(pos, rot);

            _alive.Add(instance);
            OnSpawned?.Invoke(instance);

            if (autoDespawnAfter > 0f)
                _autoDespawnRoutines[instance] = StartCoroutine(AutoDespawnAfter(instance, autoDespawnAfter));

            return instance;
        }

        private IEnumerator AutoDespawnAfter(GameObject instance, float seconds)
        {
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(seconds);
            else yield return new WaitForSeconds(seconds);

            if (instance != null)
                Despawn(instance);
        }

        private int PickPrefabIndex()
        {
            if (prefabs == null || prefabs.Length == 0) return -1;

            if (chooseRandomPrefab)
                return UnityEngine.Random.Range(0, prefabs.Length);

            int idx = _sequentialIndex;

            _sequentialIndex++;
            if (_sequentialIndex >= prefabs.Length)
                _sequentialIndex = sequentialLoop ? 0 : prefabs.Length - 1;

            return idx;
        }

        private Transform PickSpawnPoint()
        {
            Transform primary = spawnLocation != null ? spawnLocation : transform;

            if (spawnPointPick != SpawnPointPick.RandomFromList)
                return primary;

            if (extraSpawnPoints != null && extraSpawnPoints.Length > 0)
            {
                int tries = Mathf.Min(extraSpawnPoints.Length, 8);
                for (int i = 0; i < tries; i++)
                {
                    var t = extraSpawnPoints[UnityEngine.Random.Range(0, extraSpawnPoints.Length)];
                    if (t != null) return t;
                }
            }

            return primary;
        }

        private GameObject GetInstance(int prefabIndex)
        {
            if (usePooling && _pools != null && prefabIndex >= 0 && prefabIndex < _pools.Length)
            {
                var go = _pools[prefabIndex].Get();
                return go;
            }

            return Instantiate(prefabs[prefabIndex]);
        }

        private void BuildPools()
        {
            if (prefabs == null || prefabs.Length == 0) return;

            _pools = new ObjectPool<GameObject>[prefabs.Length];

            for (int i = 0; i < prefabs.Length; i++)
            {
                int index = i;
                GameObject prefab = prefabs[i];

                if (prefab == null) continue;

                _pools[i] = new ObjectPool<GameObject>(
                    createFunc: () =>
                    {
                        var go = Instantiate(prefab);
                        go.SetActive(false);

                        var tag = go.GetComponent<SpawnedByTimedSpawner>();
                        if (tag == null) tag = go.AddComponent<SpawnedByTimedSpawner>();
                        tag.Initialize(this, index);

                        return go;
                    },
                    actionOnGet: go => go.SetActive(true),
                    actionOnRelease: go =>
                    {
                        // Optional: reset velocity, state, etc. in your prefab scripts if you need.
                        go.SetActive(false);
                    },
                    actionOnDestroy: go =>
                    {
                        if (go != null) Destroy(go);
                    },
                    collectionCheck: false,
                    defaultCapacity: defaultPoolCapacity,
                    maxSize: maxPoolSize
                );
            }
        }

        private void CleanupDeadAlive()
        {
            _alive.RemoveWhere(go => go == null);
        }

        private void OnDrawGizmosSelected()
        {
            Transform p = spawnLocation != null ? spawnLocation : transform;

            Gizmos.matrix = p.localToWorldMatrix;
            Gizmos.DrawWireCube(localPositionOffset, Vector3.one * 0.1f);
            Gizmos.DrawWireCube(localPositionOffset, randomLocalPositionBox * 2f);

            if (extraSpawnPoints != null)
            {
                foreach (var t in extraSpawnPoints)
                {
                    if (t == null) continue;
                    Gizmos.matrix = t.localToWorldMatrix;
                    Gizmos.DrawWireSphere(Vector3.zero, 0.08f);
                }
            }
        }
    }

    /// <summary>
    /// Added automatically when pooling is enabled. You can also use it manually:
    /// call DespawnSelf() when an enemy dies to return it to the pool (or destroy if not pooled).
    /// </summary>
    public class SpawnedByTimedSpawner : MonoBehaviour
    {
        public TimedSpawner Spawner { get; private set; }
        public int PrefabIndex { get; private set; } = -1;

        public void Initialize(TimedSpawner spawner, int prefabIndex)
        {
            Spawner = spawner;
            PrefabIndex = prefabIndex;
        }

        public void DespawnSelf()
        {
            if (Spawner != null)
                Spawner.Despawn(gameObject);
            else
                Destroy(gameObject);
        }
    }
}