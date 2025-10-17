// ============================================================================
// SimpleMazePrefabPlacer.cs  — Hardened spawn: variants + big rooms + specials
// Guarantees: for every non-reserved cell, we spawn either a tile prefab or a
// cube fallback. Start/End rectangles are centered and auto-shrunk if they
// would reserve 100% of the grid.
// Requires: MazeCarver.cs (hardened).
// ============================================================================

using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;

public class SimpleMazePrefabPlacer : NetworkBehaviour
{
    // -------- Run / Size ----------
    [Header("Run Mode")]
    public bool generateOnStart = true;
    public bool anchorStartAtWorldOrigin = true;

    [Header("Grid Sizing")]
    public bool sizeFromWorldMeters = true;
    public Vector2 worldSizeMeters = new Vector2(80, 80);
    public Vector2 roomSizeMeters = new Vector2(4, 4);
    [Min(2)] public int width = 20;    // used if sizeFromWorldMeters == false
    [Min(2)] public int height = 20;

    [Header("Randomness")]
    public int seed = 0;                // 0 = random

    // -------- Variants (weighted lists) ----------
    [Serializable] public class WeightedPrefab { public GameObject prefab; [Min(1)] public int weight = 1; }

    [Header("Tile Variants (random pick; set at least one OR set defaults)")]
    public List<WeightedPrefab> straightVariants = new();
    public List<WeightedPrefab> cornerVariants = new();
    public List<WeightedPrefab> tJunctionVariants = new();
    public List<WeightedPrefab> crossVariants = new();
    public List<WeightedPrefab> deadEndVariants = new();

    [Header("Default Tile Prefabs (fallback when list is empty)")]
    public GameObject defaultStraight;
    public GameObject defaultCorner;
    public GameObject defaultTJunction;
    public GameObject defaultCross;
    public GameObject defaultDeadEnd;

    // -------- Start / End (multi-cell) ----------
    [Header("Start / End Rooms (multi-cell)")]
    public GameObject startRoomPrefab;
    public Vector2Int startRoomSizeCells = new Vector2Int(2, 2);
    [Min(1)] public int startRoomDoorCount = 1;

    public GameObject endRoomPrefab;
    public Vector2Int endRoomSizeCells = new Vector2Int(2, 2);
    [Min(1)] public int endRoomDoorCount = 1;

    // -------- Specials ----------
    [Serializable]
    public class SpecialRoom
    {
        public string name;
        public GameObject prefab;
        public Vector2Int sizeCells = new Vector2Int(3, 2);
        [Min(0)] public int count = 1;

        [Tooltip("If true, picks random free anchors; otherwise uses Fixed Anchor (clamped).")]
        public bool placeRandomly = true;
        public Vector2Int fixedAnchorCell = new Vector2Int(1, 1);

        [Min(1)] public int doorwayCount = 1;

        [HideInInspector] public List<Vector2Int> placedAnchors = new();
    }

    [Header("Special Rooms (multi-cell)")]
    public List<SpecialRoom> specialRooms = new();

    // -------- Parenting / Debug ----------
    [Header("Parent")]
    public Transform mazeParent;

    [Header("Debug")]
    public bool verboseLogging = true;
    public bool drawGizmos = false;
    public bool visualizeReservedCells = false;

    // ---- runtime ----
    private MazeCarver.Result result;
    private System.Random rng;
    private bool[,] reserved;  // cells covered by big rooms (skip standard tiles)

    // warn flags
    private bool wStraight, wCorner, wT, wCross, wDead;

    void Awake()
    {
        if (!mazeParent)
        {
            var go = new GameObject("Maze");
            go.transform.SetParent(transform, false);
            mazeParent = go.transform;
        }
    }
    /*
        void Start()
        {
            if (generateOnStart)
            {
                Generate();
                GetComponent<NavMeshSurface>().BuildNavMesh();
            }

        }*/


    public override void OnNetworkSpawn()
    {

        Generate();
        GetComponent<NavMeshSurface>().BuildNavMesh();
    }

    [ContextMenu("Generate Now")]
    public void Generate()
    {
        // Derive width/height from world meters if requested
        if (sizeFromWorldMeters)
        {
            float sx = Mathf.Max(0.01f, roomSizeMeters.x);
            float sz = Mathf.Max(0.01f, roomSizeMeters.y);
            width = Mathf.Max(2, Mathf.FloorToInt(worldSizeMeters.x / sx));
            height = Mathf.Max(2, Mathf.FloorToInt(worldSizeMeters.y / sz));
        }

        if (!ValidateInputs()) return;

        ClearMaze();

        rng = (seed == 0) ? new System.Random(Guid.NewGuid().GetHashCode())
                          : new System.Random(seed);

        // 1) Carve a hardened maze (no outside leaks, single component)
        result = MazeCarver.Generate(width, height, seed);

        // 2) Reserve big rectangles (Start/End/Specials), open interiors, punch doorways
        reserved = new bool[width, height];

        // Compute start/end rectangles centered on their target cells
        var startSize = ClampSize(startRoomSizeCells);
        var startAnchor = AnchorRectContaining(result.start, startSize);

        var endSize = ClampSize(endRoomSizeCells);
        var endAnchor = AnchorRectContaining(result.end, endSize);

        // Apply, then ensure we didn't reserve 100% of the grid.
        ReserveRect(startAnchor, startSize, mark: true);
        ReserveRect(endAnchor, endSize, mark: true);

        // If everything ended up reserved (tiny grids + big rooms), shrink Start/End to 1x1
        if (CountReservedCells() >= width * height)
        {
            if (verboseLogging) Debug.LogWarning("[SimpleMazePrefabPlacer] Start/End sizes reserved the whole grid. Auto-shrinking to 1x1.");
            Array.Clear(reserved, 0, reserved.Length); // reset reservations
            startSize = new Vector2Int(1, 1);
            endSize = new Vector2Int(1, 1);
            startAnchor = AnchorRectContaining(result.start, startSize);
            endAnchor = AnchorRectContaining(result.end, endSize);
            ReserveRect(startAnchor, startSize, mark: true);
            ReserveRect(endAnchor, endSize, mark: true);
        }

        // Open interiors & punch doors
        OpenInteriorWalls(startAnchor, startSize);
        PunchDoorwaysIntoMaze(startAnchor, startSize, Mathf.Max(1, startRoomDoorCount));

        OpenInteriorWalls(endAnchor, endSize);
        PunchDoorwaysIntoMaze(endAnchor, endSize, Mathf.Max(1, endRoomDoorCount));

        // Specials (skip if they can't fit — TryReserveRect prevents overlap)
        foreach (var sr in specialRooms)
        {
            sr.placedAnchors.Clear();
            var size = ClampSize(sr.sizeCells);
            int placed = 0, attempts = 0, maxAttempts = width * height * 3;

            while (placed < sr.count && attempts++ < maxAttempts)
            {
                Vector2Int anchor = sr.placeRandomly
                    ? new Vector2Int(rng.Next(0, width), rng.Next(0, height))
                    : AnchorRectContaining(sr.fixedAnchorCell, size);

                if (TryReserveRect(anchor, size))
                {
                    sr.placedAnchors.Add(anchor);
                    OpenInteriorWalls(anchor, size);
                    PunchDoorwaysIntoMaze(anchor, size, Math.Max(1, sr.doorwayCount));
                    placed++;
                    if (!sr.placeRandomly) break; // only once if fixed
                }

                if (!sr.placeRandomly) break; // couldn't place at fixed spot
            }
        }

        // 3) Spawn prefabs (or cube fallback) for every non-reserved cell
        int tilesSpawned = BuildPrefabs(startAnchor, startSize, endAnchor, endSize);

        if (verboseLogging)
        {
            int reservedCount = CountReservedCells();
            Debug.Log($"[SimpleMazePrefabPlacer] Generated. Grid {width}x{height} ({width * height} cells). Reserved {reservedCount}. Spawned tiles {tilesSpawned}. Start@{result.start} end@{result.end}");
        }
    }

    [ContextMenu("Clear Maze")]
    public void ClearMaze()
    {
        if (!mazeParent) return;
        var list = new List<GameObject>();
        foreach (Transform c in mazeParent) list.Add(c.gameObject);
        foreach (var go in list)
        {
            if (Application.isEditor) DestroyImmediate(go);
            else Destroy(go);
        }
    }

    // ----------------------------------------------------------------------
    // Reservation & Doorways
    // ----------------------------------------------------------------------
    Vector2Int ClampSize(Vector2Int sz) =>
        new Vector2Int(Mathf.Clamp(sz.x, 1, width), Mathf.Clamp(sz.y, 1, height));

    // Compute the min-corner (anchor) for a rectangle of 'size' that CONTAINS 'cell'
    // by centering first, then clamping to grid bounds.
    Vector2Int AnchorRectContaining(Vector2Int cell, Vector2Int size)
    {
        size = ClampSize(size);
        int ax = cell.x - (size.x - 1) / 2;
        int ay = cell.y - (size.y - 1) / 2;
        ax = Mathf.Clamp(ax, 0, width - size.x);
        ay = Mathf.Clamp(ay, 0, height - size.y);
        return new Vector2Int(ax, ay);
    }

    int CountReservedCells()
    {
        int c = 0;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (reserved[x, y]) c++;
        return c;
    }

    // Try to reserve a rectangle of cells for a big/special room.
    // Returns true if successful, false if out of bounds or overlapping.
    bool TryReserveRect(Vector2Int anchor, Vector2Int size)
    {
        size = ClampSize(size);
        if (anchor.x < 0 || anchor.y < 0 || anchor.x + size.x > width || anchor.y + size.y > height)
            return false;

        // Check overlap
        for (int dx = 0; dx < size.x; dx++)
            for (int dy = 0; dy < size.y; dy++)
            {
                int x = anchor.x + dx, y = anchor.y + dy;
                if (reserved[x, y]) return false;
            }

        // Reserve
        ReserveRect(anchor, size, mark: true);
        return true;
    }

    // Mark a rectangle as reserved (skip standard tiles there)
    void ReserveRect(Vector2Int anchor, Vector2Int size, bool mark)
    {
        for (int dx = 0; dx < size.x; dx++)
            for (int dy = 0; dy < size.y; dy++)
            {
                int x = anchor.x + dx, y = anchor.y + dy;
                if (x < 0 || x >= width || y < 0 || y >= height) continue;
                if (mark) reserved[x, y] = true;
            }
    }

    // Remove interior walls inside a reserved rectangle so it’s one open room.
    void OpenInteriorWalls(Vector2Int anchor, Vector2Int size)
    {
        var g = result.grid;
        for (int dx = 0; dx < size.x; dx++)
            for (int dy = 0; dy < size.y; dy++)
            {
                int x = anchor.x + dx, y = anchor.y + dy;
                if (x < 0 || x >= width || y < 0 || y >= height) continue;

                if (dy + 1 < size.y) { g.cells[x, y].N = false; g.cells[x, y + 1].S = false; }
                if (dx + 1 < size.x) { g.cells[x, y].E = false; g.cells[x + 1, y].W = false; }
            }
    }

    // Punch 'count' openings from the rectangle’s border cells into the surrounding maze.
    void PunchDoorwaysIntoMaze(Vector2Int anchor, Vector2Int size, int count)
    {
        var g = result.grid;
        var borderCandidates = new List<Action>();

        for (int dx = 0; dx < size.x; dx++)
            for (int dy = 0; dy < size.y; dy++)
            {
                int x = anchor.x + dx, y = anchor.y + dy;

                // N edge
                if (dy == size.y - 1 && y + 1 < height)
                    borderCandidates.Add(() => { g.cells[x, y].N = false; g.cells[x, y + 1].S = false; });
                // S edge
                if (dy == 0 && y - 1 >= 0)
                    borderCandidates.Add(() => { g.cells[x, y].S = false; g.cells[x, y - 1].N = false; });
                // E edge
                if (dx == size.x - 1 && x + 1 < width)
                    borderCandidates.Add(() => { g.cells[x, y].E = false; g.cells[x + 1, y].W = false; });
                // W edge
                if (dx == 0 && x - 1 >= 0)
                    borderCandidates.Add(() => { g.cells[x, y].W = false; g.cells[x - 1, y].E = false; });
            }

        if (borderCandidates.Count == 0) return;

        int need = Mathf.Clamp(count, 1, borderCandidates.Count);
        for (int i = 0; i < need; i++)
        {
            int pick = rng.Next(0, borderCandidates.Count);
            borderCandidates[pick]();
            borderCandidates.RemoveAt(pick);
        }
    }

    // ----------------------------------------------------------------------
    // Build Prefabs / Fallbacks
    // ----------------------------------------------------------------------
    int BuildPrefabs(Vector2Int startAnchor, Vector2Int startSize, Vector2Int endAnchor, Vector2Int endSize)
    {
        wStraight = wCorner = wT = wCross = wDead = false;

        // 1) Place Start/End big prefabs at the center of their rectangles (or simple cubes if missing)
        var startCenter = RectCenterWorld(startAnchor, startSize);
        if (startRoomPrefab)
        {
            var s = Instantiate(startRoomPrefab, startCenter, Quaternion.identity, mazeParent);
            s.name = $"StartRoom_{startAnchor.x}_{startAnchor.y}_{startSize.x}x{startSize.y}";
        }
        else
        {
            SpawnCube($"StartRoom_Fallback_{startAnchor.x}_{startAnchor.y}",
                      startCenter, new Vector3(startSize.x * roomSizeMeters.x, 1f, startSize.y * roomSizeMeters.y));
            if (verboseLogging) Debug.LogWarning("[SimpleMazePrefabPlacer] Start room prefab missing; used cube fallback.");
        }

        var endCenter = RectCenterWorld(endAnchor, endSize);
        if (endRoomPrefab)
        {
            var e = Instantiate(endRoomPrefab, endCenter, Quaternion.identity, mazeParent);
            e.name = $"EndRoom_{endAnchor.x}_{endAnchor.y}_{endSize.x}x{endSize.y}";
        }
        else
        {
            SpawnCube($"EndRoom_Fallback_{endAnchor.x}_{endAnchor.y}",
                      endCenter, new Vector3(endSize.x * roomSizeMeters.x, 1f, endSize.y * roomSizeMeters.y));
            if (verboseLogging) Debug.LogWarning("[SimpleMazePrefabPlacer] End room prefab missing; used cube fallback.");
        }

        // 2) Standard tiles for all non-reserved cells
        var grid = result.grid;
        int spawned = 0;

        for (int x = 0; x < grid.width; x++)
            for (int y = 0; y < grid.height; y++)
            {
                if (reserved[x, y]) continue;

                int mask = grid.OpenMask(x, y);
                int degree = MazeCarver.Grid.DegreeFromMask(mask);

                GameObject prefab = null; float rotY = 0f;

                switch (degree)
                {
                    case 1:
                        prefab = Pick(deadEndVariants, defaultDeadEnd, ref wDead, "DeadEnd");
                        if ((mask & 1) != 0) rotY = 180f;   // open left
                        else if ((mask & 2) != 0) rotY = 0f;     // open right
                        else if ((mask & 4) != 0) rotY = 90f;    // open down
                        else if ((mask & 8) != 0) rotY = -90f;   // open up
                        break;

                    case 2:
                        bool lr = ((mask & 1) != 0) && ((mask & 2) != 0);
                        bool ud = ((mask & 4) != 0) && ((mask & 8) != 0);
                        if (lr || ud)
                        {
                            prefab = Pick(straightVariants, defaultStraight, ref wStraight, "Straight");
                            rotY = ud ? 90f : 0f;
                        }
                        else
                        {
                            prefab = Pick(cornerVariants, defaultCorner, ref wCorner, "Corner");
                            if ((mask & 2) != 0 && (mask & 8) != 0) rotY = 0f;    // right + up
                            else if ((mask & 1) != 0 && (mask & 8) != 0) rotY = -90f;  // left  + up
                            else if ((mask & 1) != 0 && (mask & 4) != 0) rotY = 180f;  // left  + down
                            else if ((mask & 2) != 0 && (mask & 4) != 0) rotY = 90f;   // right + down
                        }
                        break;

                    case 3:
                        prefab = Pick(tJunctionVariants, defaultTJunction, ref wT, "T-Junction");
                        if ((mask & 1) == 0) rotY = 0f;     // missing left
                        else if ((mask & 2) == 0) rotY = 180f;   // missing right
                        else if ((mask & 4) == 0) rotY = -90f;   // missing down
                        else if ((mask & 8) == 0) rotY = 90f;    // missing up
                        break;

                    case 4:
                        prefab = Pick(crossVariants, defaultCross, ref wCross, "Cross");
                        rotY = 0f;
                        break;

                    default:
                        // degree == 0: shouldn't happen with MazeCarver
                        break;
                }

                var pos = CellToWorld(new Vector2Int(x, y));

                if (prefab != null)
                {
                    var go = Instantiate(prefab, pos, Quaternion.Euler(0, rotY, 0), mazeParent);
                    go.name = $"Tile_{x}_{y}";
                    spawned++;
                }
                else
                {
                    // ALWAYS fallback so you can see a maze even if prefabs weren’t set
                    SpawnCube($"TileFallback_{x}_{y}", pos, new Vector3(roomSizeMeters.x, 0.2f, roomSizeMeters.y));
                    spawned++;
                }
            }

        if (spawned == 0 && verboseLogging)
            Debug.LogWarning("[SimpleMazePrefabPlacer] No tiles spawned. Check that Start/End didn’t reserve the whole grid or that your grid isn’t extremely small.");

        return spawned;
    }

    // Weighted pick with fallback; filters null entries; sets one-time warnings
    GameObject Pick(List<WeightedPrefab> list, GameObject fallback, ref bool warned, string label)
    {
        var pool = new List<WeightedPrefab>();
        if (list != null)
        {
            foreach (var wp in list)
                if (wp != null && wp.prefab != null && wp.weight > 0)
                    pool.Add(wp);
        }

        if (pool.Count > 0)
        {
            int total = 0; foreach (var wp in pool) total += wp.weight;
            int r = rng.Next(total);
            foreach (var wp in pool)
            {
                if (r < wp.weight) return wp.prefab;
                r -= wp.weight;
            }
            return pool[0].prefab; // safety
        }

        if (fallback) return fallback;

        if (!warned)
        {
            Debug.LogWarning($"[SimpleMazePrefabPlacer] No prefab available for {label}. Assign variants or a default.");
            warned = true;
        }
        return null;
    }

    // ----------------------------------------------------------------------
    // Position helpers
    // ----------------------------------------------------------------------
    Vector3 CellToWorld(Vector2Int cell)
    {
        var local = new Vector3(cell.x * roomSizeMeters.x, 0f, cell.y * roomSizeMeters.y);
        if (anchorStartAtWorldOrigin) return local;
        return transform.TransformPoint(local);
    }

    Vector3 RectCenterWorld(Vector2Int anchor, Vector2Int size)
    {
        var min = CellToWorld(anchor);
        float offX = (size.x - 1) * 0.5f * roomSizeMeters.x;
        float offZ = (size.y - 1) * 0.5f * roomSizeMeters.y;
        return min + new Vector3(offX, 0f, offZ);
    }

    // ----------------------------------------------------------------------
    // Validation
    // ----------------------------------------------------------------------
    bool ValidateInputs()
    {
        bool ok = true;

        if (roomSizeMeters.x <= 0f || roomSizeMeters.y <= 0f)
        { Debug.LogError("[SimpleMazePrefabPlacer] Room Size must be > 0."); ok = false; }

        if (width < 2 || height < 2)
        { Debug.LogError("[SimpleMazePrefabPlacer] Width/Height must be >= 2."); ok = false; }

        return ok;
    }

    // ----------------------------------------------------------------------
    // Gizmos / Helpers
    // ----------------------------------------------------------------------
    void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        if (visualizeReservedCells && reserved != null)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (reserved[x, y])
                        Gizmos.DrawCube(CellToWorld(new Vector2Int(x, y)),
                            new Vector3(roomSizeMeters.x * 0.9f, 0.1f, roomSizeMeters.y * 0.9f));
        }
    }

    // Spawn a simple cube fallback (always available)
    void SpawnCube(string name, Vector3 center, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(mazeParent, false);
        go.transform.position = center;
        go.transform.localScale = size;
        var col = go.GetComponent<Collider>(); if (!col) go.AddComponent<BoxCollider>();
    }
}
