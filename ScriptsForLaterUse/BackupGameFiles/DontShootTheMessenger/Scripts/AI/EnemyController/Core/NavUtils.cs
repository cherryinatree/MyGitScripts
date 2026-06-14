using UnityEngine;
using UnityEngine.AI;

public static class NavUtils
{
    /// <summary> Sample the nearest position on the NavMesh near 'pos'. </summary>
    public static bool Sample(Vector3 pos, out Vector3 hitPos, float maxDistance = 2f, int areaMask = NavMesh.AllAreas)
    {
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, maxDistance, areaMask))
        {
            hitPos = hit.position;
            return true;
        }
        hitPos = pos;
        return false;
    }

    /// <summary> Pick a random reachable point around 'origin' on the NavMesh. </summary>
    public static bool RandomReachablePoint(Vector3 origin, float radius, out Vector3 result, int areaMask = NavMesh.AllAreas)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 r = Random.insideUnitCircle * radius;
            Vector3 candidate = origin + new Vector3(r.x, 0f, r.y);
            if (Sample(candidate, out result, radius, areaMask)) return true;
        }
        result = origin;
        return false;
    }
}
