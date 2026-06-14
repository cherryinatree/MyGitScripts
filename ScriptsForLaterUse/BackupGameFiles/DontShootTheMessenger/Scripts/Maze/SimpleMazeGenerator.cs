using System;
using System.Collections.Generic;
using UnityEngine;

public class SimpleMazeGenerator : MonoBehaviour
{
    [Header("Grid (small to start)")]
    [Min(2)] public int width = 15;
    [Min(2)] public int height = 15;
    public float cellSize = 4f;
    public int seed = 0;               // 0 = random

    [Header("Geometry")]
    public GameObject cubePrefab;      // optional; if null, uses Unity primitive cube
    public float wallHeight = 2.2f;
    public float wallThickness = 0.25f;
    public bool addCeiling = false;

    [Header("Materials (optional)")]
    public Material floorMat;
    public Material wallMat;
    public Material ceilingMat;

    [Header("Parents (auto-created if null)")]
    public Transform mazeParent;

    [Header("Start/End Markers (optional)")]
    public GameObject startMarkerPrefab;
    public GameObject endMarkerPrefab;

    // --- runtime state ---
    Cell[,] cells;
    System.Random rng;
    Vector2Int start;
    Vector2Int end;

    [Serializable]
    class Cell
    {
        public bool visited;
        // walls: N,E,S,W (clockwise, starting up/north)
        public bool N = true, E = true, S = true, W = true;
    }

    void Awake()
    {
        if (!mazeParent)
        {
            var p = new GameObject("Maze");
            p.transform.SetParent(transform, false);
            mazeParent = p.transform;
        }
    }

    void Start()
    {
        Generate();
    }

    [ContextMenu("Generate Now")]
    public void Generate()
    {
        ClearMaze();

        rng = (seed == 0) ? new System.Random(Guid.NewGuid().GetHashCode()) : new System.Random(seed);
        cells = new Cell[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                cells[x, y] = new Cell();

        // 1) carve with DFS (perfect maze)
        start = new Vector2Int(0, 0);                 // top-left; easy to reason about
        CarveFrom(start);

        // 2) choose END as farthest reachable cell for a nice long path
        end = FarthestFrom(start);

        // 3) build simple cube geometry
        BuildGeometry();

        // 4) optional markers
        if (startMarkerPrefab)
            Instantiate(startMarkerPrefab, CellToWorld(start) + Vector3.up * 0.01f, Quaternion.identity, mazeParent);
        if (endMarkerPrefab)
            Instantiate(endMarkerPrefab, CellToWorld(end) + Vector3.up * 0.01f, Quaternion.identity, mazeParent);
    }

    void CarveFrom(Vector2Int c)
    {
        var stack = new Stack<Vector2Int>();
        stack.Push(c);
        cells[c.x, c.y].visited = true;

        while (stack.Count > 0)
        {
            var cur = stack.Peek();
            var neighbors = UnvisitedNeighbors(cur);
            if (neighbors.Count == 0)
            {
                stack.Pop();
                continue;
            }

            var next = neighbors[rng.Next(neighbors.Count)];
            RemoveWall(cur, next);
            cells[next.x, next.y].visited = true;
            stack.Push(next);
        }
    }

    List<Vector2Int> UnvisitedNeighbors(Vector2Int c)
    {
        var list = new List<Vector2Int>(4);
        void AddIf(int x, int y)
        {
            if (x >= 0 && x < width && y >= 0 && y < height && !cells[x, y].visited)
                list.Add(new Vector2Int(x, y));
        }
        AddIf(c.x, c.y + 1); // N
        AddIf(c.x + 1, c.y); // E
        AddIf(c.x, c.y - 1); // S
        AddIf(c.x - 1, c.y); // W
        return list;
    }

    void RemoveWall(Vector2Int a, Vector2Int b)
    {
        var d = b - a;
        if (d == Vector2Int.up) { cells[a.x, a.y].N = false; cells[b.x, b.y].S = false; }
        if (d == Vector2Int.right) { cells[a.x, a.y].E = false; cells[b.x, b.y].W = false; }
        if (d == Vector2Int.down) { cells[a.x, a.y].S = false; cells[b.x, b.y].N = false; }
        if (d == Vector2Int.left) { cells[a.x, a.y].W = false; cells[b.x, b.y].E = false; }
    }

    Vector2Int FarthestFrom(Vector2Int from)
    {
        // BFS distances along carved passages
        var dist = new int[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
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

            var cell = cells[c.x, c.y];
            void Enq(int x, int y, bool open)
            {
                if (!open) return;
                if (x < 0 || x >= width || y < 0 || y >= height) return;
                if (dist[x, y] != -1) return;
                dist[x, y] = d + 1;
                q.Enqueue(new Vector2Int(x, y));
            }
            Enq(c.x, c.y + 1, !cell.N); // north open means connection upwards
            Enq(c.x + 1, c.y, !cell.E);
            Enq(c.x, c.y - 1, !cell.S);
            Enq(c.x - 1, c.y, !cell.W);
        }
        return far;
    }

    // ---------- Geometry (cubes) ----------
    GameObject SpawnCube(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject cube = cubePrefab
            ? Instantiate(cubePrefab, mazeParent)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);

        cube.name = name;
        cube.transform.SetParent(mazeParent, false);
        cube.transform.position = pos;
        cube.transform.localScale = scale;

        var r = cube.GetComponentInChildren<MeshRenderer>();
        if (r && mat) r.sharedMaterial = mat;

        // add simple collider if prefab didn’t have one
        if (!cube.GetComponentInChildren<Collider>())
            cube.AddComponent<BoxCollider>();

        return cube;
    }

    void BuildGeometry()
    {
        // floors
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var world = CellToWorld(new Vector2Int(x, y));
                // floor is a thin cube
                SpawnCube($"Floor_{x}_{y}",
                    world + new Vector3(0, -wallThickness * 0.5f, 0),
                    new Vector3(cellSize, wallThickness, cellSize),
                    floorMat);

                if (addCeiling)
                {
                    SpawnCube($"Ceiling_{x}_{y}",
                        world + new Vector3(0, wallHeight + wallThickness * 0.5f, 0),
                        new Vector3(cellSize, wallThickness, cellSize),
                        ceilingMat);
                }
            }

        // walls (per cell; only build N and W to avoid duplicates, plus border E/S)
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var world = CellToWorld(new Vector2Int(x, y));
                var c = cells[x, y];

                float hs = cellSize * 0.5f;

                // North wall for this cell
                if (c.N)
                    SpawnCube($"W_N_{x}_{y}",
                        world + new Vector3(0, wallHeight * 0.5f, +hs),
                        new Vector3(cellSize, wallHeight, wallThickness),
                        wallMat);

                // West wall
                if (c.W)
                    SpawnCube($"W_W_{x}_{y}",
                        world + new Vector3(-hs, wallHeight * 0.5f, 0),
                        new Vector3(wallThickness, wallHeight, cellSize),
                        wallMat);

                // East border walls on last column
                if (x == width - 1 && c.E)
                    SpawnCube($"W_E_{x}_{y}",
                        world + new Vector3(+hs, wallHeight * 0.5f, 0),
                        new Vector3(wallThickness, wallHeight, cellSize),
                        wallMat);

                // South border walls on last row
                if (y == 0 && c.S)
                    SpawnCube($"W_S_{x}_{y}",
                        world + new Vector3(0, wallHeight * 0.5f, -hs),
                        new Vector3(cellSize, wallHeight, wallThickness),
                        wallMat);
            }
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        // center cell at transform position
        return transform.TransformPoint(new Vector3(
            (cell.x - (width - 1) * 0.5f) * cellSize,
            0f,
            (cell.y - (height - 1) * 0.5f) * cellSize));
    }

    [ContextMenu("Clear Maze")]
    public void ClearMaze()
    {
        if (!mazeParent) return;
        var toDestroy = new List<GameObject>();
        foreach (Transform c in mazeParent) toDestroy.Add(c.gameObject);
        foreach (var go in toDestroy)
        {
            if (Application.isEditor) DestroyImmediate(go);
            else Destroy(go);
        }
    }
}
