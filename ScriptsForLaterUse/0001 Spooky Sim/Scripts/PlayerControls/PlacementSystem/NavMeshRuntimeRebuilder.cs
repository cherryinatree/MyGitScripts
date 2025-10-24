using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Unity.AI.Navigation;

public class NavMeshRuntimeRebuilder : MonoBehaviour
{
    [SerializeField] private NavMeshSurface[] surfaces;
    [SerializeField] private float debounceSeconds = 0.5f;

    static NavMeshRuntimeRebuilder _instance;
    Coroutine rebuildRoutine;

    void Awake()
    {
        _instance = this;
        Placeable.OnPlacedOrMoved += _ => RequestRebuild();
    }
    void OnDestroy()
    {
        Placeable.OnPlacedOrMoved -= _ => RequestRebuild();
        if (_instance == this) _instance = null;
    }

    public static void RequestRebuild() => _instance?._RequestRebuild();

    void _RequestRebuild()
    {
        if (rebuildRoutine != null) return;
        rebuildRoutine = StartCoroutine(RebuildAfterDelay());
    }

    IEnumerator RebuildAfterDelay()
    {
        yield return new WaitForSeconds(debounceSeconds);
        foreach (var s in surfaces) if (s) s.BuildNavMesh();
        rebuildRoutine = null;
    }
}
