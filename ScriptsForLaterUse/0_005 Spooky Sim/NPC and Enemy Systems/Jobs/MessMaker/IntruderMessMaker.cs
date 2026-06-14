using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class IntruderMessMaker : MonoBehaviour
{
    [Header("Refs")]
    public IntruderMaster Master;

    [Header("Prefabs")]
    public MessStain StainPrefab;
    public MessTrash TrashPrefab;

    [Header("Spawn")]
    public Vector2 IntervalRange = new Vector2(2.0f, 5.0f);
    [Range(0f, 1f)] public float StainChance = 0.6f;
    public float SpawnRadius = 2.0f;

    [Header("Placement")]
    public LayerMask FloorMask = ~0;
    public float RayUp = 2.0f;
    public float RayDown = 6.0f;
    public float SurfaceOffset = 0.01f;

    [Header("Limits")]
    public int MaxActiveMessFromThisIntruder = 8;

    private float _nextSpawn;

    private void Awake()
    {
        if (Master == null) Master = GetComponentInParent<IntruderMaster>() ?? GetComponent<IntruderMaster>();
        ScheduleNext();
    }

    private void Update()
    {
        if (Master == null) return;
        if (Master.Job != IntruderMaster.IntruderJob.MessMaker) return;

        // Don’t casually litter while actively threatened (they should flee)
        if (Master.HasThreat) return;

        if (Time.time < _nextSpawn) return;

        if (CountMyActiveMess() >= MaxActiveMessFromThisIntruder)
        {
            ScheduleNext();
            return;
        }

        TrySpawnOne();
        ScheduleNext();
    }

    private void ScheduleNext()
    {
        float t = Random.Range(IntervalRange.x, IntervalRange.y);
        _nextSpawn = Time.time + Mathf.Max(0.05f, t);
    }

    private int CountMyActiveMess()
    {
        int count = 0;
        var all = MessRegistry.All;
        for (int i = 0; i < all.Count; i++)
        {
            var m = all[i];
            if (m == null) continue;
            if (m.IsResolved) continue;
            if (m.SourceIntruder == Master) count++;
        }
        return count;
    }

    private void TrySpawnOne()
    {
        if (StainPrefab == null || TrashPrefab == null) return;

        // Pick a navmesh nearby
        Vector3 basePos = transform.position;
        Vector3 rnd = basePos + Random.insideUnitSphere * SpawnRadius;
        rnd.y = basePos.y;

        if (!NavMesh.SamplePosition(rnd, out var navHit, SpawnRadius + 2f, NavMesh.AllAreas))
            return;

        // Raycast down to floor for accurate placement
        Vector3 rayStart = navHit.position + Vector3.up * RayUp;
        if (!Physics.Raycast(rayStart, Vector3.down, out var hit, RayUp + RayDown, FloorMask, QueryTriggerInteraction.Ignore))
            return;

        Vector3 pos = hit.point + hit.normal * SurfaceOffset;

        bool spawnStain = Random.value <= StainChance;
        if (spawnStain)
        {
            var stain = Instantiate(StainPrefab, pos, Quaternion.identity);
            stain.SourceIntruder = Master;
            stain.Kind = MessItem.MessKind.Stain;

            // Align to surface + random yaw
            stain.transform.up = hit.normal;
            stain.transform.Rotate(Vector3.up, Random.Range(0f, 360f), Space.Self);
        }
        else
        {
            var trash = Instantiate(TrashPrefab, pos, Quaternion.identity);
            trash.SourceIntruder = Master;
            trash.Kind = MessItem.MessKind.Trash;

            trash.transform.up = hit.normal;
            trash.transform.Rotate(Vector3.up, Random.Range(0f, 360f), Space.Self);
        }
    }
}
