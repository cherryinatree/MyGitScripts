using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TransportRouter : MonoBehaviour
{
    [System.Serializable]
    public class TransportEdge
    {
        public TransportPad from;
        public TransportPad to;
        public float cost = 2f;
    }

    [Header("Optional explicit edges (custom topology)")]
    public List<TransportEdge> explicitEdges = new();

    [Header("Defaults")]
    public float defaultTransportCost = 2f;
    public float SampleRadius = 8f; // <- bump this up (6–12 is common in tall ships)


    private void DebugPads(IList<TransportPad> allPads, List<TransportPad> sources, HashSet<TransportPad> targets)
    {
        //Debug.Log($"[TransportRouter] Pads total={allPads.Count}, sources(reachable from start)={sources.Count}, targets(can reach goal)={targets.Count}");
        for (int i = 0; i < allPads.Count; i++)
        {
            var p = allPads[i];
            if (p == null) continue;
            //Debug.Log($"[TransportRouter] Pad={p.name} net='{p.networkId}' entry={p.InteractionPoint.position} exit={p.ExitPoint.position}");
        }
    }


    public List<TransportPad> FindBestPadRoute(Vector3 start, Vector3 goal, IList<TransportPad> allPads)
    {
        var sources = new List<TransportPad>();
        var sourceCost = new Dictionary<TransportPad, float>();

        var targets = new HashSet<TransportPad>();
        var targetToGoalCost = new Dictionary<TransportPad, float>();

        for (int i = 0; i < allPads.Count; i++)
        {
            var pad = allPads[i];
            if (pad == null) continue;

            float sCost = EstimateNavDistance(start, pad.InteractionPoint.position);
            if (!float.IsInfinity(sCost))
            {
                sources.Add(pad);
                sourceCost[pad] = sCost;
            }

            float gCost = EstimateNavDistance(pad.ExitPoint.position, goal);
            if (!float.IsInfinity(gCost))
            {
                targets.Add(pad);
                targetToGoalCost[pad] = gCost;
            }
        }

        DebugPads(allPads, sources, targets);
        if (sources.Count == 0 || targets.Count == 0) return null;

        var dist = new Dictionary<TransportPad, float>();
        var prev = new Dictionary<TransportPad, TransportPad>();
        var pq = new SimplePriorityQueue<TransportPad>();

        for (int i = 0; i < sources.Count; i++)
        {
            var s = sources[i];
            float d0 = sourceCost[s];
            dist[s] = d0;
            prev[s] = null;
            pq.EnqueueOrDecrease(s, d0);
        }

        TransportPad bestTarget = null;
        float bestTotal = float.PositiveInfinity;

        while (pq.Count > 0)
        {
            var u = pq.Dequeue(out float du);
            if (du >= bestTotal) continue;

            if (targets.Contains(u))
            {
                float total = du + targetToGoalCost[u];
                if (total < bestTotal)
                {
                    bestTotal = total;
                    bestTarget = u;
                }
            }

            foreach (var (v, w) in GetNeighbors(u, allPads))
            {
                float nd = du + w;
                if (!dist.TryGetValue(v, out float cur) || nd < cur)
                {
                    dist[v] = nd;
                    prev[v] = u;
                    pq.EnqueueOrDecrease(v, nd);
                }
            }
        }

        if (bestTarget == null) return null;

        var route = new List<TransportPad>();
        var node = bestTarget;
        while (node != null)
        {
            route.Add(node);
            node = prev[node];
        }
        route.Reverse();
        return route;
    }

    private IEnumerable<(TransportPad pad, float cost)> GetNeighbors(TransportPad from, IList<TransportPad> allPads)
    {
        // Network fully connected
        for (int i = 0; i < allPads.Count; i++)
        {
            var p = allPads[i];
            if (p == null || p == from) continue;
            if (p.networkId == from.networkId)
                yield return (p, defaultTransportCost);
        }

        // Explicit edges
        for (int i = 0; i < explicitEdges.Count; i++)
        {
            var e = explicitEdges[i];
            if (e == null || e.from == null || e.to == null) continue;
            if (e.from == from)
                yield return (e.to, Mathf.Max(0.01f, e.cost));
        }
    }

    public bool HasDirectPath(Vector3 from, Vector3 to)
    {
        if (!NavMesh.SamplePosition(from, out var hitA, 2f, NavMesh.AllAreas)) return false;
        if (!NavMesh.SamplePosition(to, out var hitB, 2f, NavMesh.AllAreas)) return false;

        var path = new NavMeshPath();
        if (!NavMesh.CalculatePath(hitA.position, hitB.position, NavMesh.AllAreas, path)) return false;
        return path.status == NavMeshPathStatus.PathComplete;
    }



    private float EstimateNavDistance(Vector3 from, Vector3 to)
    {
        if (!TrySample(from, out var a)) return float.PositiveInfinity;
        if (!TrySample(to, out var b)) return float.PositiveInfinity;

        var path = new NavMeshPath();
        if (!NavMesh.CalculatePath(a, b, NavMesh.AllAreas, path)) return float.PositiveInfinity;
        if (path.status != NavMeshPathStatus.PathComplete) return float.PositiveInfinity;

        float sum = 0f;
        var c = path.corners;
        for (int i = 1; i < c.Length; i++)
            sum += Vector3.Distance(c[i - 1], c[i]);

        return sum;
    }

    private bool TrySample(Vector3 pos, out Vector3 hitPos)
    {
        if (NavMesh.SamplePosition(pos, out var hit, SampleRadius, NavMesh.AllAreas))
        {
            hitPos = hit.position;
            return true;
        }

        hitPos = pos;
        return false;
    }


    private class SimplePriorityQueue<T>
    {
        private readonly List<(T item, float pri)> _list = new();
        public int Count => _list.Count;

        public void EnqueueOrDecrease(T item, float pri)
        {
            for (int i = 0; i < _list.Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(_list[i].item, item))
                {
                    if (pri < _list[i].pri) _list[i] = (item, pri);
                    return;
                }
            }
            _list.Add((item, pri));
        }

        public T Dequeue(out float pri)
        {
            int best = 0;
            float bestP = _list[0].pri;
            for (int i = 1; i < _list.Count; i++)
            {
                if (_list[i].pri < bestP)
                {
                    bestP = _list[i].pri;
                    best = i;
                }
            }
            var v = _list[best];
            _list.RemoveAt(best);
            pri = v.pri;
            return v.item;
        }
    }
}
