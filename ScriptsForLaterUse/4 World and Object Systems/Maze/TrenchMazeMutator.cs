
// ===============================
// File: TrenchMazeMutator.cs
// ===============================
using System.Collections.Generic;
using UnityEngine;

namespace TrenchMaze
{
    /// <summary>
    /// Runtime API for safe, out-of-sight maze mutations.
    /// Attach to the same GameObject as TrenchMazeGenerator.
    /// Other scripts can call RequestMutation(...) to reshuffle sections.
    /// </summary>
    [RequireComponent(typeof(TrenchMazeGenerator))]
    public class TrenchMazeMutator : MonoBehaviour
    {
        public int defaultChanges = 6;                // edges to toggle per mutation call
        public int minDistanceFromPlayerCells = 2;    // do not mutate within this many cells of protected region
        public float rebuildDelay = 0.05f;            // small delay to batch multiple calls in one frame

        TrenchMazeGenerator gen;
        bool rebuildQueued;

        void Awake() => gen = GetComponent<TrenchMazeGenerator>();

        /// <summary>
        /// Perform a mutation confined to non-protected cells. Will only close walls if it won't disconnect the graph.
        /// </summary>
        public void RequestMutation(int changes = -1)
        {
            if (gen.Surface == null) return;
            int c = (changes <= 0) ? defaultChanges : changes;
            var g = gen.Surface;
            int attempts = 0, maxAttempts = c * 10;
            var protectedSet = gen.ProtectedCells;

            while (c > 0 && attempts++ < maxAttempts)
            {
                var v = RandomCell(g);
                if (protectedSet.Contains(v)) continue;
                if (IsNearProtected(v, minDistanceFromPlayerCells)) continue;

                var nbs = g.GetCardinalNeighbors(v);
                if (nbs.Count == 0) continue;
                var n = nbs[Random.Range(0, nbs.Count)];
                if (protectedSet.Contains(n)) continue;
                if (IsNearProtected(n, minDistanceFromPlayerCells)) continue;

                bool connected = g.AreConnected(v, n);
                if (connected)
                {
                    // try to safely close if alternative path exists
                    if (HasAlternatePath(g, v, n))
                    {
                        g.Disconnect(v, n);
                        c--;
                    }
                }
                else
                {
                    // open wall to create a new loop
                    g.Connect(v, n);
                    c--;
                }
            }

            QueueRebuild();
        }

        public void RegenerateCompletely(int? newSeed = null)
        {
            if (newSeed.HasValue) gen.seed = newSeed.Value;
            gen.Generate();
        }

        Vector2Int RandomCell(GridGraph g)
        {
            // avoid borders slightly to reduce geometry popping near edges
            int x = Random.Range(1, Mathf.Max(2, gen.SurfaceParentWidth() - 1));
            int y = Random.Range(1, Mathf.Max(2, gen.SurfaceParentHeight() - 1));
            return new Vector2Int(x, y);
        }

        bool IsNearProtected(Vector2Int cell, int radius)
        {
            foreach (var p in gen.ProtectedCells)
            {
                if (Mathf.Abs(p.x - cell.x) + Mathf.Abs(p.y - cell.y) <= radius)
                    return true;
            }
            return false;
        }

        bool HasAlternatePath(GridGraph g, Vector2Int a, Vector2Int b)
        {
            // Temporarily disconnect and BFS to see if a & b remain connected.
            bool wasConnected = g.AreConnected(a, b);
            if (!wasConnected) return true;
            g.Disconnect(a, b);
            var visited = new HashSet<Vector2Int>();
            var q = new Queue<Vector2Int>();
            q.Enqueue(a); visited.Add(a);
            bool reachable = false;
            while (q.Count > 0 && !reachable)
            {
                var cur = q.Dequeue();
                foreach (var nb in g.GetCardinalNeighbors(cur))
                {
                    if (!g.IsOpen(nb) || !g.AreConnected(cur, nb)) continue;
                    if (visited.Contains(nb)) continue;
                    if (nb == b) { reachable = true; break; }
                    visited.Add(nb);
                    q.Enqueue(nb);
                }
            }
            // restore connection if it was a bridge
            if (!reachable) g.Connect(a, b);
            return reachable;
        }

        void QueueRebuild()
        {
            if (rebuildQueued) return;
            rebuildQueued = true;
            Invoke(nameof(DoRebuild), rebuildDelay);
        }

        void DoRebuild()
        {
            rebuildQueued = false;
            // Rebuild only the surface visuals; underground stays unless you mutate it too.
            gen.RebuildSurfaceOnly();
        }
    }
}
