// Put this file under an `Editor/` folder: Assets/Editor/TrenchRoomCubeBuilderWindow.cs
// Menu: Tools/Trench Maze/Cube Room Builder
// Builds room prefabs by INSTANTIATING and RESIZING CUBES (or your cube prefab),
// then assigns materials. No custom mesh generation.

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class TrenchRoomCubeBuilderWindow : EditorWindow
{
    public enum RoomType { StartRoom, EndRoom, Straight, Corner, TJunction, Cross, DeadEnd, Ladder }

    // ------------------- Inputs -------------------
    RoomType roomType = RoomType.Straight;

    [Header("Grid & Sizing")]
    float cellSize = 4f;           // one maze cell length
    float passageWidth = 2.5f;     // inside walkable width
    float wallThickness = 0.25f;   // cube depth for walls
    float wallHeight = 2.2f;
    bool addCeiling = false;

    // Corner approximation w/ cubes
    int cornerSegments = 6;        // more = smoother curve (cubes along an arc)
    float cornerRadius = 1.25f;    // inner radius of the corner arc

    // Start/End room size
    Vector2 roomSizeMultiplier = new Vector2(2f, 2f);
    int startRoomExitCount = 3;    // visual markers only

    // Materials
    Material wallMat;
    Material floorMat;
    Material ceilingMat;

    // Use a prefab cube instead of PrimitiveType.Cube (e.g., your own UVs/colliders)
    GameObject cubePrefab;         // optional; if null we use GameObject.CreatePrimitive(Cube)

    // Output
    string outputName = "TrenchRoom";

    [MenuItem("Tools/Trench Maze/Cube Room Builder")]
    public static void Open()
    {
        var win = GetWindow<TrenchRoomCubeBuilderWindow>(true, "Trench Room Builder — Cubes");
        win.minSize = new Vector2(360, 560);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Room Type", EditorStyles.boldLabel);
        roomType = (RoomType)EditorGUILayout.EnumPopup("Type", roomType);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Dimensions", EditorStyles.boldLabel);
        cellSize = EditorGUILayout.FloatField("Cell Size", Mathf.Max(0.1f, cellSize));
        passageWidth = EditorGUILayout.FloatField("Passage Width", Mathf.Clamp(passageWidth, 0.25f, cellSize * 2f));
        wallThickness = EditorGUILayout.FloatField("Wall Thickness", Mathf.Clamp(wallThickness, 0.02f, 1f));
        wallHeight = EditorGUILayout.FloatField("Wall Height", Mathf.Max(0.2f, wallHeight));
        addCeiling = EditorGUILayout.Toggle("Add Ceiling", addCeiling);
        EditorGUILayout.Space();

        if (roomType == RoomType.Corner)
        {
            EditorGUILayout.LabelField("Corner Approximation (Cubes on Arc)", EditorStyles.boldLabel);
            cornerSegments = EditorGUILayout.IntSlider("Segments", cornerSegments, 2, 24);
            cornerRadius = EditorGUILayout.FloatField("Inner Radius", Mathf.Max(0.1f, cornerRadius));
            EditorGUILayout.Space();
        }

        if (roomType == RoomType.StartRoom || roomType == RoomType.EndRoom)
        {
            EditorGUILayout.LabelField("Start/End Room", EditorStyles.boldLabel);
            roomSizeMultiplier = EditorGUILayout.Vector2Field("Size Multiplier", roomSizeMultiplier);
            if (roomType == RoomType.StartRoom)
                startRoomExitCount = EditorGUILayout.IntSlider("Visual Exit Count", startRoomExitCount, 1, 4);
            EditorGUILayout.Space();
        }

        EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);
        wallMat = (Material)EditorGUILayout.ObjectField("Wall Material", wallMat, typeof(Material), false);
        floorMat = (Material)EditorGUILayout.ObjectField("Floor Material", floorMat, typeof(Material), false);
        ceilingMat = (Material)EditorGUILayout.ObjectField("Ceiling Material", ceilingMat, typeof(Material), false);
        EditorGUILayout.Space();

        cubePrefab = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Cube Prefab (optional)", "If assigned, we'll instantiate this instead of a Unity primitive cube."), cubePrefab, typeof(GameObject), false);
        outputName = EditorGUILayout.TextField("Output Name", string.IsNullOrWhiteSpace(outputName) ? "TrenchRoom" : outputName);

        if (GUILayout.Button("Build In Scene"))
        {
            var go = BuildRoom();
            Selection.activeGameObject = go;
            SceneView.FrameLastActiveSceneView();
        }

        using (new EditorGUI.DisabledScope(Selection.activeGameObject == null))
        {
            if (GUILayout.Button("Save Selected As Prefab…"))
            {
                string path = EditorUtility.SaveFilePanelInProject("Save Prefab", Selection.activeGameObject.name + ".prefab", "prefab", "Choose save location");
                if (!string.IsNullOrEmpty(path))
                {
                    PrefabUtility.SaveAsPrefabAssetAndConnect(Selection.activeGameObject, path, InteractionMode.UserAction);
                }
            }
        }

        EditorGUILayout.HelpBox("This builder ONLY spawns and rescales cubes (or your cube prefab). Use materials above for walls/floor/ceiling.", MessageType.Info);
    }

    // ------------------- Build Dispatcher -------------------
    GameObject BuildRoom()
    {
        var root = new GameObject(outputName + "_" + roomType);
        Undo.RegisterCreatedObjectUndo(root, "Create Room");

        switch (roomType)
        {
            case RoomType.Straight: BuildStraight(root.transform); break;
            case RoomType.DeadEnd: BuildDeadEnd(root.transform); break;
            case RoomType.Corner: BuildCorner(root.transform); break;
            case RoomType.TJunction: BuildTJunction(root.transform); break;
            case RoomType.Cross: BuildCross(root.transform); break;
            case RoomType.StartRoom: BuildRectRoom(root.transform, true); break;
            case RoomType.EndRoom: BuildRectRoom(root.transform, false); break;
            case RoomType.Ladder: BuildLadderStub(root.transform); break;
        }

        return root;
    }

    // ------------------- Cube Helpers -------------------
    GameObject SpawnCube(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject cube;
        if (cubePrefab != null)
        {
            cube = (GameObject)PrefabUtility.InstantiatePrefab(cubePrefab);
            Undo.RegisterCreatedObjectUndo(cube, "Instantiate Cube Prefab");
            cube.transform.SetParent(parent, false);
        }
        else
        {
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(cube, "Create Cube");
            cube.transform.SetParent(parent, false);
        }

        cube.name = name;
        cube.transform.localPosition = localPos;
        cube.transform.localScale = localScale;

        // Apply material on the top-level renderer if present; otherwise, try child
        var r = cube.GetComponentInChildren<MeshRenderer>();
        if (r != null && mat != null) r.sharedMaterial = mat;

        return cube;
    }

    void BuildFloor(Transform parent, float sizeX, float sizeZ)
    {
        // Thin, scaled cube as a floor plate
        SpawnCube("Floor", parent, Vector3.zero, new Vector3(sizeX, wallThickness, sizeZ), floorMat)
            .transform.localPosition = new Vector3(0, -wallThickness * 0.5f, 0);
    }

    void BuildCeiling(Transform parent, float sizeX, float sizeZ)
    {
        SpawnCube("Ceiling", parent, new Vector3(0, wallHeight + wallThickness * 0.5f, 0), new Vector3(sizeX, wallThickness, sizeZ), ceilingMat);
    }

    // Wall along X span (centered across X, positioned at given Z edge)
    void BuildWallAlongX(Transform parent, float spanX, float atZ)
    {
        SpawnCube("Wall_X", parent, new Vector3(0, wallHeight * 0.5f, atZ), new Vector3(spanX, wallHeight, wallThickness), wallMat);
    }

    // Wall along Z span (centered across Z, positioned at given X edge)
    void BuildWallAlongZ(Transform parent, float spanZ, float atX)
    {
        SpawnCube("Wall_Z", parent, new Vector3(atX, wallHeight * 0.5f, 0), new Vector3(wallThickness, wallHeight, spanZ), wallMat);
    }

    // ------------------- Builders -------------------
    void BuildStraight(Transform parent)
    {
        float L = cellSize;        // corridor length
        float W = passageWidth;    // corridor inner width
        float halfL = L * 0.5f;
        float halfW = W * 0.5f;

        BuildFloor(parent, L, W);
        BuildWallAlongX(parent, L, +halfW + wallThickness * 0.5f); // right
        BuildWallAlongX(parent, L, -halfW - wallThickness * 0.5f); // left
        if (addCeiling) BuildCeiling(parent, L, W);
    }

    void BuildDeadEnd(Transform parent)
    {
        BuildStraight(parent);
        float W = passageWidth;
        float halfW = W * 0.5f;
        // Back cap at -X end
        SpawnCube("CapWall", parent, new Vector3(-cellSize * 0.5f - wallThickness * 0.5f, wallHeight * 0.5f, 0), new Vector3(wallThickness, wallHeight, W + 2f * wallThickness), wallMat);
    }

    void BuildCorner(Transform parent)
    {
        // Approximate a 90° corner with cubes placed along an arc between +X and +Z directions.
        float inner = Mathf.Max(0.01f, cornerRadius);
        float outer = inner + passageWidth;
        int segs = Mathf.Max(2, cornerSegments);

        // Floor slab that bounds the arc rectangle
        float sizeX = outer;
        float sizeZ = outer;
        BuildFloor(parent, sizeX, sizeZ);
        if (addCeiling) BuildCeiling(parent, sizeX, sizeZ);

        // Build inner and outer arc walls using small cube segments
        for (int i = 0; i < segs; i++)
        {
            float t0 = i / (float)segs;
            float t1 = (i + 1) / (float)segs;
            float a0 = Mathf.Lerp(0f, 90f, t0) * Mathf.Deg2Rad;
            float a1 = Mathf.Lerp(0f, 90f, t1) * Mathf.Deg2Rad;
            float mid = (a0 + a1) * 0.5f;
            float chordOuter = Vector2.Distance(new Vector2(Mathf.Cos(a0) * outer, Mathf.Sin(a0) * outer), new Vector2(Mathf.Cos(a1) * outer, Mathf.Sin(a1) * outer));
            float chordInner = Vector2.Distance(new Vector2(Mathf.Cos(a0) * inner, Mathf.Sin(a0) * inner), new Vector2(Mathf.Cos(a1) * inner, Mathf.Sin(a1) * inner));

            // Outer wall segment
            Vector3 posOuter = new Vector3(Mathf.Cos(mid) * outer, wallHeight * 0.5f, Mathf.Sin(mid) * outer);
            Vector3 scaleOuter = new Vector3(chordOuter, wallHeight, wallThickness);
            var segOut = SpawnCube($"Wall_Outer_{i}", parent, posOuter, scaleOuter, wallMat);
            segOut.transform.rotation = Quaternion.Euler(0, Mathf.Rad2Deg * mid + 90f, 0);

            // Inner wall segment
            Vector3 posInner = new Vector3(Mathf.Cos(mid) * inner, wallHeight * 0.5f, Mathf.Sin(mid) * inner);
            Vector3 scaleInner = new Vector3(chordInner, wallHeight, wallThickness);
            var segIn = SpawnCube($"Wall_Inner_{i}", parent, posInner, scaleInner, wallMat);
            segIn.transform.rotation = Quaternion.Euler(0, Mathf.Rad2Deg * mid + 90f, 0);
        }
    }

    void BuildTJunction(Transform parent)
    {
        // Main straight
        BuildStraight(parent);
        float arm = cellSize * 0.5f;
        float W = passageWidth;
        float halfW = W * 0.5f;

        // Perpendicular arm centered around x=0 (extends to +Z)
        BuildFloor(parent, W, arm);
        SpawnCube("Wall_T_L", parent, new Vector3(-halfW - wallThickness * 0.5f, wallHeight * 0.5f, arm * 0.5f), new Vector3(wallThickness, wallHeight, arm), wallMat);
        SpawnCube("Wall_T_R", parent, new Vector3(+halfW + wallThickness * 0.5f, wallHeight * 0.5f, arm * 0.5f), new Vector3(wallThickness, wallHeight, arm), wallMat);
        if (addCeiling) BuildCeiling(parent, W, arm);
    }

    void BuildCross(Transform parent)
    {
        // Two perpendicular straights intersecting
        BuildStraight(parent);
        float L = cellSize;
        float W = passageWidth;
        BuildFloor(parent, W, L);
        SpawnCube("Wall_Cross_L", parent, new Vector3(-W * 0.5f - wallThickness * 0.5f, wallHeight * 0.5f, 0), new Vector3(wallThickness, wallHeight, L), wallMat);
        SpawnCube("Wall_Cross_R", parent, new Vector3(+W * 0.5f + wallThickness * 0.5f, wallHeight * 0.5f, 0), new Vector3(wallThickness, wallHeight, L), wallMat);
        if (addCeiling) BuildCeiling(parent, W, L);
    }

    void BuildRectRoom(Transform parent, bool isStart)
    {
        float sizeX = cellSize * Mathf.Max(1f, roomSizeMultiplier.x);
        float sizeZ = cellSize * Mathf.Max(1f, roomSizeMultiplier.y);
        float halfX = sizeX * 0.5f;
        float halfZ = sizeZ * 0.5f;

        BuildFloor(parent, sizeX, sizeZ);
        // Perimeter walls
        BuildWallAlongX(parent, sizeX, +halfZ + wallThickness * 0.5f); // North
        BuildWallAlongX(parent, sizeX, -halfZ - wallThickness * 0.5f); // South
        BuildWallAlongZ(parent, sizeZ, -halfX - wallThickness * 0.5f); // West
        BuildWallAlongZ(parent, sizeZ, +halfX + wallThickness * 0.5f); // East
        if (addCeiling) BuildCeiling(parent, sizeX, sizeZ);

        if (isStart)
        {
            for (int i = 0; i < Mathf.Clamp(startRoomExitCount, 1, 4); i++)
            {
                float t = (i + 0.5f) / Mathf.Clamp(startRoomExitCount, 1, 4);
                var marker = SpawnCube($"ExitMarker_{i}", parent, Vector3.zero, Vector3.one * 0.25f, wallMat);
                switch (i % 4)
                {
                    case 0: marker.transform.localPosition = new Vector3(Mathf.Lerp(-halfX, halfX, t), 0.125f, -halfZ - wallThickness); break;
                    case 1: marker.transform.localPosition = new Vector3(Mathf.Lerp(-halfX, halfX, t), 0.125f, +halfZ + wallThickness); break;
                    case 2: marker.transform.localPosition = new Vector3(-halfX - wallThickness, 0.125f, Mathf.Lerp(-halfZ, halfZ, t)); break;
                    case 3: marker.transform.localPosition = new Vector3(+halfX + wallThickness, 0.125f, Mathf.Lerp(-halfZ, halfZ, t)); break;
                }
            }
        }
    }

    void BuildLadderStub(Transform parent)
    {
        SpawnCube("LadderStub", parent, new Vector3(0, wallHeight * 0.5f, 0), new Vector3(0.2f, wallHeight, 0.2f), wallMat);
    }
}
#endif
