// ===============================
// File: TrenchMazeGenerator.cs
// ===============================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrenchMaze
{
    [DisallowMultipleComponent]
    public class TrenchMazeGenerator : MonoBehaviour
    {
        [Header("Grid")]
        public int width = 21;          // odd numbers recommended
        public int height = 21;         // odd numbers recommended
        public float cellSize = 4f;     // trench tile spacing (meters)
        public int seed = 0;            // 0 = random at runtime
        public bool generateOnStart = true;

        [Header("Topology & Paths")]
        [Range(0, 1)] public float loopChance = 0.08f; // % chance to add extra openings (creates loops)
        [Min(0)] public int guaranteedLoops = 8;        // always add at least this many loops
        public bool endRoomFarthestFromStart = true;    // choose end room as farthest reachable cell

        [Header("Optional Underground Floor")]
        public bool generateUnderground = false; // set true to auto-generate a 2nd layer
        [Range(0, 1)] public float ladderDensity = 0.015f; // ladders connecting floors (only where safe)
        public GameObject ladderPrefab;  // place centered in a cell; rotate/offset via child if needed

        [Header("Room & Trench Prefabs")]
        public GameObject startRoomPrefab;
        public GameObject endRoomPrefab;
        public GameObject straightPrefab;
        public GameObject cornerPrefab;
        public GameObject tJunctionPrefab;
        public GameObject crossPrefab;
        public GameObject deadEndPrefab;

        [Header("Parents (auto-created if null)")]
        public Transform surfaceParent;
        public Transform undergroundParent;

        [Header("Debug & Gizmos")]
        public bool drawGizmos = false;
        public Color startColor = new(0.2f, 0.9f, 0.4f, 0.85f);
        public Color endColor = new(0.9f, 0.3f, 0.2f, 0.85f);
        public Color protectedColor = new(1f, 0.8f, 0.2f, 0.5f);

        // Runtime state
        System.Random rng;
        GridGraph surface;
        GridGraph underground;
        Vector2Int startCell;
        Vector2Int endCell;
        HashSet<Vector2Int> protectedCells = new(); // start+end and their neighbors (no dynamic edits)

        public GridGraph Surface => surface;
        public GridGraph Underground => underground;
        public Vector2Int StartCell => startCell;
        public Vector2Int EndCell => endCell;
        public HashSet<Vector2Int> ProtectedCells => protectedCells;

        void Awake()
        {
            if (surfaceParent == null)
            {
                var go = new GameObject("Surface");
                go.transform.SetParent(transform, false);
                surfaceParent = go.transform;
            }
            if (undergroundParent == null)
            {
                var go = new GameObject("Underground");
                go.transform.SetParent(transform, false);
                undergroundParent = go.transform;
            }
        }

        void Start()
        {
            if (generateOnStart)
                Generate();
        }

        [ContextMenu("Generate Now")]
        public void Generate()
        {
            ClearLevel();

            rng = (seed == 0) ? new System.Random(Guid.NewGuid().GetHashCode()) : new System.Random(seed);
            surface = new GridGraph(width, height);

            // 1) Build base maze with 3 distinct exits from the start cell
            startCell = PickStartCell();
            CarveMazeWithThreeBranches(surface, startCell);

            // 2) Choose end cell (farthest)
            endCell = endRoomFarthestFromStart ? FarthestFrom(surface, startCell) : PickEndCellFarFromStart(0.6f);

            // 3) Add loops for multiple routes
            AddLoops(surface, guaranteedLoops);
            AddRandomLoops(surface, loopChance);

            // 4) Compute protected cells (start, end, and their immediate neighbors)
            ComputeProtectedCells();

            // 5) Build scene geometry for surface
            BuildLevel(surface, surfaceParent, 0);

            // 6) Optional underground generation
            if (generateUnderground)
            {
                underground = new GridGraph(width, height);
                // Generate a different but related layout (offset seed slightly)
                var saveRng = rng;
                rng = new System.Random((saveRng.Next() ^ 0x5F3759DF) & int.MaxValue);
                var uStart = PickStartCell();
                CarveMazeWithThreeBranches(underground, uStart);
                AddLoops(underground, Mathf.Max(guaranteedLoops / 2, 4));
                AddRandomLoops(underground, loopChance * 0.8f);
                rng = saveRng;

                BuildLevel(underground, undergroundParent, -cellSize); // shift down one cellSize for clarity
                PlaceLaddersBetweenFloors();
            }
        }

        // =============== Generation Internals ===============
        Vector2Int PickStartCell()
        {
            // Prefer a cell with >= 3 neighbors available, biasing to edges but not corners
            List<Vector2Int> candidates = new();
            for (int x = 1; x < width - 1; x++)
                for (int y = 1; y < height - 1; y++)
                {
                    var v = new Vector2Int(x, y);
                    if (CountCardinalNeighbors(v) >= 3) candidates.Add(v);
                }
            if (candidates.Count == 0) return new Vector2Int(width / 2, height / 2);
            return candidates[rng.Next(candidates.Count)];
        }

        Vector2Int PickEndCellFarFromStart(float minNormalizedDistance)
        {
            var farList = new List<Vector2Int>();
            foreach (var v in surface.AllCells())
            {
                float d = Mathf.Abs(v.x - startCell.x) + Mathf.Abs(v.y - startCell.y);
                float maxD = (width - 1) + (height - 1);
                if (d / maxD >= minNormalizedDistance)
                    farList.Add(v);
            }
            return farList.Count > 0 ? farList[rng.Next(farList.Count)] : new Vector2Int(width - 2, height - 2);
        }

        Vector2Int FarthestFrom(GridGraph g, Vector2Int from)
        {
            var dist = g.BfsDistances(from);
            int best = -1; Vector2Int bestCell = from;
            foreach (var kv in dist)
            {
                if (kv.Value > best)
                {
                    best = kv.Value; bestCell = kv.Key;
                }
            }
            return bestCell;
        }

        int CountCardinalNeighbors(Vector2Int v)
        {
            int c = 0;
            if (v.x > 0) c++; if (v.x < width - 1) c++; if (v.y > 0) c++; if (v.y < height - 1) c++;
            return c;
        }

        void CarveMazeWithThreeBranches(GridGraph g, Vector2Int start)
        {
            g.FillWalls();

            // Choose 3 distinct directions from start
            var dirs = new List<Vector2Int>();
            if (start.x > 0) dirs.Add(Vector2Int.left);
            if (start.x < width - 1) dirs.Add(Vector2Int.right);
            if (start.y > 0) dirs.Add(Vector2Int.down);
            if (start.y < height - 1) dirs.Add(Vector2Int.up);
            Shuffle(dirs);
            int needed = Mathf.Min(3, dirs.Count);
            var initialNeighbors = new List<Vector2Int>();
            for (int i = 0; i < needed; i++) initialNeighbors.Add(start + dirs[i]);

            // Use a Prim-like frontier expansion seeded by those 3 neighbors
            var frontier = new List<Vector2Int>();
            g.OpenCell(start);
            foreach (var nb in initialNeighbors)
            {
                if (g.InBounds(nb))
                {
                    g.Connect(start, nb);
                    frontier.Add(nb);
                }
            }

            while (frontier.Count > 0)
            {
                int idx = rng.Next(frontier.Count);
                var cur = frontier[idx];
                frontier.RemoveAt(idx);

                var nbs = g.GetCardinalNeighbors(cur);
                Shuffle(nbs);
                bool connectedToOpen = false;
                foreach (var n in nbs)
                {
                    if (g.IsOpen(n))
                    {
                        if (!g.AreConnected(cur, n))
                        {
                            g.Connect(cur, n);
                        }
                        connectedToOpen = true;
                        break;
                    }
                }
                if (!connectedToOpen)
                {
                    // Connect to a random open cell in neighborhood to keep it growing
                    var openNeighbors = nbs.FindAll(g.IsOpen);
                    if (openNeighbors.Count > 0)
                        g.Connect(cur, openNeighbors[rng.Next(openNeighbors.Count)]);
                    else
                        g.OpenCell(cur);
                }

                foreach (var n in nbs)
                {
                    if (!g.IsOpen(n))
                    {
                        g.OpenCell(n);
                        frontier.Add(n);
                    }
                }
            }
        }

        void AddLoops(GridGraph g, int count)
        {
            for (int i = 0; i < count; i++)
            {
                TryAddRandomOpening(g);
            }
        }

        void AddRandomLoops(GridGraph g, float chance)
        {
            foreach (var v in g.AllCells())
            {
                if (rng.NextDouble() < chance)
                    TryAddRandomOpening(g, v);
            }
        }

        void TryAddRandomOpening(GridGraph g, Vector2Int? cellOpt = null)
        {
            var cell = cellOpt ?? new Vector2Int(rng.Next(1, width - 1), rng.Next(1, height - 1));
            var nbs = g.GetCardinalNeighbors(cell);
            if (nbs.Count == 0) return;
            var n = nbs[rng.Next(nbs.Count)];
            g.Connect(cell, n); // idempotent if already connected
        }

        void ComputeProtectedCells()
        {
            protectedCells.Clear();
            protectedCells.Add(startCell);
            protectedCells.Add(endCell);
            foreach (var n in surface.GetCardinalNeighbors(startCell)) protectedCells.Add(n);
            foreach (var n in surface.GetCardinalNeighbors(endCell)) protectedCells.Add(n);
        }

        void BuildLevel(GridGraph g, Transform parent, float yOffset)
        {
            // Clear previous children under target parent
            var toDestroy = new List<Transform>();
            foreach (Transform c in parent) toDestroy.Add(c);
            for (int i = toDestroy.Count - 1; i >= 0; i--) DestroyImmediate(toDestroy[i].gameObject);

            // Place rooms first
            PlaceRoom(startRoomPrefab, startCell, parent, yOffset, startColor);
            PlaceRoom(endRoomPrefab, endCell, parent, yOffset, endColor);

            // Place trench tiles based on degree (open sides)
            foreach (var v in g.AllCells())
            {
                if (v == startCell || v == endCell) continue;
                int mask = g.OpenMask(v); // bits: 1=L,2=R,4=D,8=U
                int degree = g.Degree(v);

                GameObject prefab = null;
                float rot = 0f;

                switch (degree)
                {
                    case 1:
                        prefab = deadEndPrefab;
                        // rotate so the open side points out of the cap
                        if ((mask & 1) != 0) rot = 180f;          // open left => cap faces left, rotate 180
                        else if ((mask & 2) != 0) rot = 0f;      // open right
                        else if ((mask & 4) != 0) rot = 90f;     // open down
                        else if ((mask & 8) != 0) rot = -90f;    // open up
                        break;
                    case 2:
                        if (((mask & 1) != 0 && (mask & 2) != 0) || ((mask & 4) != 0 && (mask & 8) != 0))
                        {
                            prefab = straightPrefab; // opposite sides => straight
                            rot = ((mask & 4) != 0 && (mask & 8) != 0) ? 90f : 0f; // vertical vs horizontal
                        }
                        else
                        {
                            prefab = cornerPrefab; // adjacent => corner
                            // Determine rotation so corner bends correctly
                            if ((mask & 2) != 0 && (mask & 8) != 0) rot = 0f;        // right + up
                            else if ((mask & 1) != 0 && (mask & 8) != 0) rot = -90f; // left + up
                            else if ((mask & 1) != 0 && (mask & 4) != 0) rot = 180f; // left + down
                            else if ((mask & 2) != 0 && (mask & 4) != 0) rot = 90f;  // right + down
                        }
                        break;
                    case 3:
                        prefab = tJunctionPrefab;
                        // Rotate so the missing side becomes the cap
                        if ((mask & 1) == 0) rot = 0f;        // missing left
                        else if ((mask & 2) == 0) rot = 180f; // missing right
                        else if ((mask & 4) == 0) rot = -90f; // missing down
                        else if ((mask & 8) == 0) rot = 90f;  // missing up
                        break;
                    case 4:
                        prefab = crossPrefab;
                        rot = 0f;
                        break;
                }

                if (prefab != null)
                {
                    var pos = CellToWorld(v, yOffset);
                    var go = Instantiate(prefab, pos, Quaternion.Euler(0, rot, 0), parent);
                    go.name = $"Tile_{v.x}_{v.y}";
                }
            }
        }

        void PlaceRoom(GameObject prefab, Vector2Int cell, Transform parent, float yOffset, Color gizColor)
        {
            if (!prefab) return;
            var pos = CellToWorld(cell, yOffset);
            var go = Instantiate(prefab, pos, Quaternion.identity, parent);
            go.name = (prefab == startRoomPrefab) ? "StartRoom" : (prefab == endRoomPrefab ? "EndRoom" : prefab.name);
        }

        void PlaceLaddersBetweenFloors()
        {
            if (!ladderPrefab || underground == null) return;
            foreach (var v in surface.AllCells())
            {
                if (protectedCells.Contains(v)) continue; // never in protected region
                if (UnityEngine.Random.value > ladderDensity) continue;
                // Only place if both floors have degree >=2 to avoid trapping player
                if (surface.Degree(v) >= 2 && underground.Degree(v) >= 2)
                {
                    var up = Instantiate(ladderPrefab, CellToWorld(v, 0), Quaternion.identity, surfaceParent);
                    up.name = $"Ladder_Up_{v.x}_{v.y}";
                    var down = Instantiate(ladderPrefab, CellToWorld(v, -cellSize), Quaternion.identity, undergroundParent);
                    down.name = $"Ladder_Down_{v.x}_{v.y}";
                }
            }
        }

        public Vector3 CellToWorld(Vector2Int cell, float yOffset = 0f)
        {
            return transform.TransformPoint(new Vector3(cell.x * cellSize, yOffset, cell.y * cellSize));
        }

        public Vector2Int WorldToCell(Vector3 world)
        {
            var local = transform.InverseTransformPoint(world);
            int x = Mathf.RoundToInt(local.x / cellSize);
            int y = Mathf.RoundToInt(local.z / cellSize);
            return new Vector2Int(Mathf.Clamp(x, 0, width - 1), Mathf.Clamp(y, 0, height - 1));
        }

        public void ClearLevel()
        {
            void ClearChildren(Transform t)
            {
                if (!t) return;
                var list = new List<GameObject>();
                foreach (Transform c in t) list.Add(c.gameObject);
                foreach (var go in list) if (Application.isEditor) DestroyImmediate(go); else Destroy(go);
            }
            ClearChildren(surfaceParent);
            ClearChildren(undergroundParent);
        }

        void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (!drawGizmos || surface == null) return;
            Gizmos.matrix = transform.localToWorldMatrix;

            // Protected cells
            Gizmos.color = protectedColor;
            foreach (var v in protectedCells)
            {
                var p = new Vector3(v.x * cellSize, 0, v.y * cellSize);
                Gizmos.DrawCube(p + Vector3.up * 0.05f, Vector3.one * (cellSize * 0.4f));
            }

            // Start & End markers
            Gizmos.color = startColor;
            Gizmos.DrawSphere(new Vector3(startCell.x * cellSize, 0.25f, startCell.y * cellSize), cellSize * 0.2f);
            Gizmos.color = endColor;
            Gizmos.DrawSphere(new Vector3(endCell.x * cellSize, 0.25f, endCell.y * cellSize), cellSize * 0.2f);
        }
    }

    // ===============================
    // Lightweight grid graph for 4-neighborhood carving
    // ===============================
    [Serializable]
    public class GridGraph
    {
        readonly int w, h;
        // bitmask open-edges per cell: bit0=L, bit1=R, bit2=D, bit3=U, bit4=openSelf (visited)
        readonly byte[] open;

        public GridGraph(int width, int height)
        {
            w = Mathf.Max(3, width);
            h = Mathf.Max(3, height);
            open = new byte[w * h];
        }

        public bool InBounds(Vector2Int v) => v.x >= 0 && v.x < w && v.y >= 0 && v.y < h;
        int Idx(Vector2Int v) => v.y * w + v.x;

        public void FillWalls()
        {
            Array.Fill(open, (byte)0);
        }

        public void OpenCell(Vector2Int v)
        {
            if (!InBounds(v)) return;
            open[Idx(v)] |= 16; // mark visited/open cell
        }

        public bool IsOpen(Vector2Int v) => InBounds(v) && (open[Idx(v)] & 16) != 0;

        public void Connect(Vector2Int a, Vector2Int b)
        {
            if (!InBounds(a) || !InBounds(b)) return;
            OpenCell(a); OpenCell(b);
            var d = b - a;
            if (d == Vector2Int.left)
            { open[Idx(a)] |= 1; open[Idx(b)] |= 2; }
            else if (d == Vector2Int.right)
            { open[Idx(a)] |= 2; open[Idx(b)] |= 1; }
            else if (d == Vector2Int.down)
            { open[Idx(a)] |= 4; open[Idx(b)] |= 8; }
            else if (d == Vector2Int.up)
            { open[Idx(a)] |= 8; open[Idx(b)] |= 4; }
        }

        public bool AreConnected(Vector2Int a, Vector2Int b)
        {
            var d = b - a;
            if (d == Vector2Int.left) return (open[Idx(a)] & 1) != 0 && (open[Idx(b)] & 2) != 0;
            if (d == Vector2Int.right) return (open[Idx(a)] & 2) != 0 && (open[Idx(b)] & 1) != 0;
            if (d == Vector2Int.down) return (open[Idx(a)] & 4) != 0 && (open[Idx(b)] & 8) != 0;
            if (d == Vector2Int.up) return (open[Idx(a)] & 8) != 0 && (open[Idx(b)] & 4) != 0;
            return false;
        }
        public void Disconnect(Vector2Int a, Vector2Int b)
        {
            var d = b - a;
            if (d == Vector2Int.left)
            {
                open[Idx(a)] &= unchecked((byte)~1);
                open[Idx(b)] &= unchecked((byte)~2);
            }
            else if (d == Vector2Int.right)
            {
                open[Idx(a)] &= unchecked((byte)~2);
                open[Idx(b)] &= unchecked((byte)~1);
            }
            else if (d == Vector2Int.down)
            {
                open[Idx(a)] &= unchecked((byte)~4);
                open[Idx(b)] &= unchecked((byte)~8);
            }
            else if (d == Vector2Int.up)
            {
                open[Idx(a)] &= unchecked((byte)~8);
                open[Idx(b)] &= unchecked((byte)~4);
            }
        }
        public int OpenMask(Vector2Int v) => open[Idx(v)] & 0x0F;
        public int Degree(Vector2Int v)
        {
            int m = OpenMask(v);
            int d = 0; while (m != 0) { d += m & 1; m >>= 1; }
            return d;
        }

        public IEnumerable<Vector2Int> AllCells()
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    yield return new Vector2Int(x, y);
        }

        public List<Vector2Int> GetCardinalNeighbors(Vector2Int v)
        {
            var list = new List<Vector2Int>(4);
            if (v.x > 0) list.Add(new Vector2Int(v.x - 1, v.y));
            if (v.x < w - 1) list.Add(new Vector2Int(v.x + 1, v.y));
            if (v.y > 0) list.Add(new Vector2Int(v.x, v.y - 1));
            if (v.y < h - 1) list.Add(new Vector2Int(v.x, v.y + 1));
            return list;
        }

        public Dictionary<Vector2Int, int> BfsDistances(Vector2Int from)
        {
            var dist = new Dictionary<Vector2Int, int>(w * h);
            var q = new Queue<Vector2Int>();
            q.Enqueue(from); dist[from] = 0;
            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                foreach (var nb in GetCardinalNeighbors(cur))
                {
                    if (!IsOpen(nb) || !AreConnected(cur, nb)) continue;
                    if (dist.ContainsKey(nb)) continue;
                    dist[nb] = dist[cur] + 1;
                    q.Enqueue(nb);
                }
            }
            return dist;
        }
    }
}
