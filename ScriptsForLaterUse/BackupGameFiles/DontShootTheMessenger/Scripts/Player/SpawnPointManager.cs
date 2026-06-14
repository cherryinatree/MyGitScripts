// SpawnPointManager.cs
using UnityEngine;

public class SpawnPointManager : MonoBehaviour
{
    public static SpawnPointManager Instance;
    public Transform[] points;
    int next;

    void Awake() => Instance = this;

    public (Vector3 pos, Quaternion rot) GetNext()
    {
        Debug.Log("Getting next spawn point");
        if (points == null || points.Length == 0)
            return (Vector3.zero, Quaternion.identity);

        var t = points[next % points.Length];
        next++;
        return (t.position, t.rotation);
    }

    // Optional: draw gizmos
    void OnDrawGizmos()
    {
        if (points == null) return;
        Gizmos.color = Color.cyan;
        foreach (var p in points) if (p) Gizmos.DrawSphere(p.position, 0.25f);
    }
}
