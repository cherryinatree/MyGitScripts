// ===============================================================
// MazeCarver.cs  (Hardened)
// ---------------------------------------------------------------
// Guarantees for callers:
// - No openings to the outside border (all outer walls are closed).
// - The carved passages form ONE connected component.
// - START (0,0) and END are ALWAYS connected (END is chosen AFTER fixes).
//
// Extras:
// - Connectivity repair step to merge any stray components.
// - Optional debug validation helpers.
// ===============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

public static class MazeCarver
{
    // ---------- Data Model ----------
    [Serializable]
    public class Cell
    {
        public bool visited;
        // true = wall present; false = passage
        public bool N = true, E = true, S = true, W = true;
    }

    [Serializable]
    public class Grid
    {
        public readonly int width;
        public readonly int height;
        public readonly Cell[,] cells;

        public Grid(int w, int h)
        {
            width = Mathf.Max(2, w);
            height = Mathf.Max(2, h);
            cells = new Cell[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    cells[x, y] = new Cell();
        }

        /// Bitmask of OPEN sides for a cell:
        /// 1=Left(W −X), 2=Right(E +X), 4=Down(S −Z), 8=Up(N +Z)
        public int OpenMask(int x, int y)
        {
            int m = 0;
            var c = cells[x, y];
            if (!c.W) m |= 1;
            if (!c.E) m |= 2;
            if (!c.S) m |= 4;
            if (!c.N) m |= 8;
            return m;
        }

        public static int DegreeFromMask(int mask)
        {
            int d = 0; mask &= 0x0F;
            while (mask != 0) { d += (mask & 1); mask >>= 1; }
            return d;
        }
    }

    public struct Result
    {
        public Grid grid;
        public Vector2Int start; // always (0,0)
        public Vector2Int end;   // farthest from start AFTER fixes
    }

    // ---------- Public Entry Point ----------
    /// <summary>
    /// Generate a perfect maze, then harden it (seal borders, ensure single component),
    /// then choose END as the farthest cell from START.
    /// </summary>
    public static Result Generate(int width, int height, int seed)
    {
        var rng = (seed == 0)
            ? new System.Random(Guid.NewGuid().GetHashCode())
            : new System.Random(seed);

        var grid = new Grid(width, height);
        var start = new Vector2Int(0, 0);

        // 1) Carve a standard perfect maze (DFS backtracker)
        CarveDFS(grid, start, rng);

        // 2) Force borders to be sealed (no outside openings)
        SealBorders(grid);

        // 3) Ensure single connected component from START (repair if needed)
        EnsureConnectivityFromStart(grid, start, rng);

        // 4) Pick END as farthest from START (post-fix)
        var end = FarthestFrom(grid, start);

        // (Optional) final sanity checks
        // ValidateNoOutsideOpenings(grid, logIfIssue:true);
        // ValidateConnected(grid, start, logIfIssue:true);

        return new Result { grid = grid, start = start, end = end };
    }

    // ---------- Core Algorithms ----------
    // Depth-First Search backtracker: perfect maze (no cycles, fully connected)
    static void CarveDFS(Grid g, Vector2Int start, System.Random rng)
    {
        var stack = new Stack<Vector2Int>();
        stack.Push(start);
        g.cells[start.x, start.y].visited = true;

        while (stack.Count > 0)
        {
            var cur = stack.Peek();
            var nbs = UnvisitedNeighbors(g, cur);
            if (nbs.Count == 0) { stack.Pop(); continue; }

            var next = nbs[rng.Next(nbs.Count)];
            KnockDownWallsBetween(g, cur, next);
            g.cells[next.x, next.y].visited = true;
            stack.Push(next);
        }
    }

    static List<Vector2Int> UnvisitedNeighbors(Grid g, Vector2Int c)
    {
        var list = new List<Vector2Int>(4);
        void AddIf(int x, int y)
        {
            if (x >= 0 && x < g.width && y >= 0 && y < g.height && !g.cells[x, y].visited)
                list.Add(new Vector2Int(x, y));
        }
        // Orientation: y+1 = North (+Z), x+1 = East (+X)
        AddIf(c.x, c.y + 1); // N
        AddIf(c.x + 1, c.y); // E
        AddIf(c.x, c.y - 1); // S
        AddIf(c.x - 1, c.y); // W
        return list;
    }

    static void KnockDownWallsBetween(Grid g, Vector2Int a, Vector2Int b)
    {
        var d = b - a;
        if (d == Vector2Int.up) { g.cells[a.x, a.y].N = false; g.cells[b.x, b.y].S = false; }
        if (d == Vector2Int.right) { g.cells[a.x, a.y].E = false; g.cells[b.x, b.y].W = false; }
        if (d == Vector2Int.down) { g.cells[a.x, a.y].S = false; g.cells[b.x, b.y].N = false; }
        if (d == Vector2Int.left) { g.cells[a.x, a.y].W = false; g.cells[b.x, b.y].E = false; }
    }

    // Breadth-First Search to find the farthest cell (long, interesting path)
    static Vector2Int FarthestFrom(Grid g, Vector2Int from)
    {
        var dist = new int[g.width, g.height];
        for (int x = 0; x < g.width; x++)
            for (int y = 0; y < g.height; y++)
                dist[x, y] = -1;

        var q = new Queue<Vector2Int>();
        q.Enqueue(from);
        dist[from.x, from.y] = 0;

        Vector2Int far = from;

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            int d = dist[c.x, c.y];
            if (d > dist[far.x, far.y]) far = c;

            var cell = g.cells[c.x, c.y];
            void Enq(int x, int y, bool open)
            {
                if (!open) return;
                if (x < 0 || x >= g.width || y < 0 || y >= g.height) return;
                if (dist[x, y] != -1) return;
                dist[x, y] = d + 1;
                q.Enqueue(new Vector2Int(x, y));
            }
            Enq(c.x, c.y + 1, !cell.N); // N
            Enq(c.x + 1, c.y, !cell.E); // E
            Enq(c.x, c.y - 1, !cell.S); // S
            Enq(c.x - 1, c.y, !cell.W); // W
        }

        return far;
    }

    // ---------- Hardening Steps ----------
    /// <summary>Closes all outer walls to guarantee no openings to the outside.</summary>
    static void SealBorders(Grid g)
    {
        // Top row: close N
        for (int x = 0; x < g.width; x++)
            g.cells[x, g.height - 1].N = true;

        // Bottom row: close S
        for (int x = 0; x < g.width; x++)
            g.cells[x, 0].S = true;

        // Left col: close W
        for (int y = 0; y < g.height; y++)
            g.cells[0, y].W = true;

        // Right col: close E
        for (int y = 0; y < g.height; y++)
            g.cells[g.width - 1, y].E = true;
    }

    /// <summary>
    /// Ensures every cell is reachable from START by incrementally connecting
    /// unreachable cells to the reachable set (knocks down one wall for each
    /// disjoint region). Robust to later external edits.
    /// </summary>
    static void EnsureConnectivityFromStart(Grid g, Vector2Int start, System.Random rng)
    {
        // 1) BFS from start to find the current reachable set
        var reachable = new bool[g.width, g.height];
        BFSMarkReachable(g, start, reachable);

        // 2) While there exists an unreachable cell, connect it by opening one wall to a reachable neighbor
        int safety = g.width * g.height; // upper bound on needed merges
        while (true)
        {
            Vector2Int? unreachableCell = null;
            for (int y = 0; y < g.height && unreachableCell == null; y++)
                for (int x = 0; x < g.width && unreachableCell == null; x++)
                    if (!reachable[x, y]) unreachableCell = new Vector2Int(x, y);

            if (unreachableCell == null) break; // all connected

            var uc = unreachableCell.Value;

            // Find any neighbor of uc that is already reachable; if none, try another unreachable cell.
            var candidates = new List<(Vector2Int neighbor, Action knock)>(4);

            // N
            if (uc.y + 1 < g.height && reachable[uc.x, uc.y + 1])
                candidates.Add((new Vector2Int(uc.x, uc.y + 1), () => { g.cells[uc.x, uc.y].N = false; g.cells[uc.x, uc.y + 1].S = false; }));
            // E
            if (uc.x + 1 < g.width && reachable[uc.x + 1, uc.y])
                candidates.Add((new Vector2Int(uc.x + 1, uc.y), () => { g.cells[uc.x, uc.y].E = false; g.cells[uc.x + 1, uc.y].W = false; }));
            // S
            if (uc.y - 1 >= 0 && reachable[uc.x, uc.y - 1])
                candidates.Add((new Vector2Int(uc.x, uc.y - 1), () => { g.cells[uc.x, uc.y].S = false; g.cells[uc.x, uc.y - 1].N = false; }));
            // W
            if (uc.x - 1 >= 0 && reachable[uc.x - 1, uc.y])
                candidates.Add((new Vector2Int(uc.x - 1, uc.y), () => { g.cells[uc.x, uc.y].W = false; g.cells[uc.x - 1, uc.y].E = false; }));

            if (candidates.Count > 0)
            {
                // Pick a neighbor to connect to (random for variety)
                var choice = candidates[rng.Next(candidates.Count)];
                choice.knock();
            }
            else
            {
                // No adjacent reachable neighbor? Then try to expand the reachable set first
                // by connecting between two unreachable neighbors to form a path to the border of the reachable region.
                // (This is very rare with standard flow, but keeps us robust.)
                var merged = false;
                // Prefer knocking between uc and one of its valid neighbors to reduce isolation.
                if (uc.y + 1 < g.height) { g.cells[uc.x, uc.y].N = false; g.cells[uc.x, uc.y + 1].S = false; merged = true; }
                else if (uc.x + 1 < g.width) { g.cells[uc.x, uc.y].E = false; g.cells[uc.x + 1, uc.y].W = false; merged = true; }
                else if (uc.y - 1 >= 0) { g.cells[uc.x, uc.y].S = false; g.cells[uc.x, uc.y - 1].N = false; merged = true; }
                else if (uc.x - 1 >= 0) { g.cells[uc.x, uc.y].W = false; g.cells[uc.x - 1, uc.y].E = false; merged = true; }

                if (!merged)
                    break; // shouldn't happen, but avoid infinite loop
            }

            // Recompute reachable after each merge
            for (int x = 0; x < g.width; x++)
                for (int y = 0; y < g.height; y++)
                    reachable[x, y] = false;

            BFSMarkReachable(g, start, reachable);

            if (--safety <= 0) { Debug.LogWarning("[MazeCarver] Connectivity repair hit safety limit."); break; }
        }
    }

    static void BFSMarkReachable(Grid g, Vector2Int from, bool[,] mark)
    {
        var q = new Queue<Vector2Int>();
        q.Enqueue(from);
        mark[from.x, from.y] = true;

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            var cell = g.cells[c.x, c.y];

            void Enq(int x, int y, bool open)
            {
                if (!open) return;
                if (x < 0 || x >= g.width || y < 0 || y >= g.height) return;
                if (mark[x, y]) return;
                mark[x, y] = true;
                q.Enqueue(new Vector2Int(x, y));
            }

            Enq(c.x, c.y + 1, !cell.N);
            Enq(c.x + 1, c.y, !cell.E);
            Enq(c.x, c.y - 1, !cell.S);
            Enq(c.x - 1, c.y, !cell.W);
        }
    }

    // ---------- Optional Validators (for debugging) ----------
    static bool ValidateNoOutsideOpenings(Grid g, bool logIfIssue)
    {
        bool ok = true;
        // Top row (y = height-1) must have N closed
        for (int x = 0; x < g.width; x++) if (!g.cells[x, g.height - 1].N) { ok = false; if (logIfIssue) Debug.LogWarning($"[MazeCarver] Outside opening at TOP border x={x}"); }
        // Bottom row (y = 0) must have S closed
        for (int x = 0; x < g.width; x++) if (!g.cells[x, 0].S) { ok = false; if (logIfIssue) Debug.LogWarning($"[MazeCarver] Outside opening at BOTTOM border x={x}"); }
        // Left col (x = 0) must have W closed
        for (int y = 0; y < g.height; y++) if (!g.cells[0, y].W) { ok = false; if (logIfIssue) Debug.LogWarning($"[MazeCarver] Outside opening at LEFT border y={y}"); }
        // Right col (x = width-1) must have E closed
        for (int y = 0; y < g.height; y++) if (!g.cells[g.width - 1, y].E) { ok = false; if (logIfIssue) Debug.LogWarning($"[MazeCarver] Outside opening at RIGHT border y={y}"); }
        return ok;
    }

    static bool ValidateConnected(Grid g, Vector2Int start, bool logIfIssue)
    {
        var mark = new bool[g.width, g.height];
        BFSMarkReachable(g, start, mark);
        for (int x = 0; x < g.width; x++)
            for (int y = 0; y < g.height; y++)
                if (!mark[x, y])
                {
                    if (logIfIssue) Debug.LogWarning($"[MazeCarver] Disconnected cell at ({x},{y})");
                    return false;
                }
        return true;
    }
}
