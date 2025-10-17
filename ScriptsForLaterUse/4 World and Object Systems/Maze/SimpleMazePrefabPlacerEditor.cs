// ===============================================================
// SimpleMazePrefabPlacerEditor.cs
// ---------------------------------------------------------------
// Adds “Generate Maze (Edit Mode)” and “Clear Maze” buttons to
// the inspector so you can build/clear without entering Play.
// ===============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimpleMazePrefabPlacer))]
public class SimpleMazePrefabPlacerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(8);

        EditorGUILayout.LabelField("Editor Controls", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generate Maze (Edit Mode)", GUILayout.Height(26)))
            {
                var gen = (SimpleMazePrefabPlacer)target;
                Undo.RegisterFullObjectHierarchyUndo(gen.gameObject, "Generate Maze");
                gen.Generate();
                EditorUtility.SetDirty(gen);
            }

            if (GUILayout.Button("Clear Maze", GUILayout.Height(26)))
            {
                var gen = (SimpleMazePrefabPlacer)target;
                Undo.RegisterFullObjectHierarchyUndo(gen.gameObject, "Clear Maze");
                gen.ClearMaze();
                EditorUtility.SetDirty(gen);
            }
        }

        EditorGUILayout.HelpBox(
            "Baseline orientation (prefabs at 0°):\n" +
            "• Straight: open +Z/-Z\n" +
            "• Corner: open +Z & +X\n" +
            "• T: open +Z/+X/-X (cap at -Z)\n" +
            "• Cross: open all four\n" +
            "• DeadEnd: open +Z",
            MessageType.Info);
    }
}
#endif
