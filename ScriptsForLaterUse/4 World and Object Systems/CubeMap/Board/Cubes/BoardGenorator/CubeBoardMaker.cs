using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CubeMaster))]
public class CubeBoardMaker : Editor
{




    public override void OnInspectorGUI()
    { 
        DrawDefaultInspector();

        if (GUILayout.Button("Generate Map"))
        {

            CubeMaster cube = (CubeMaster)target;
            cube.MakeBoard();
        }
        if (GUILayout.Button("Clear Map"))
        {

            CubeMaster cube = (CubeMaster)target;
            cube.ClearBoard();
        }
    }
}
