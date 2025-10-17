using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//[CustomEditor(typeof(CustomPanel))]
public class CustomPanelEditor : Editor
{
    /*
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUI.BeginChangeCheck();

        base.OnInspectorGUI();

        if(EditorGUI.EndChangeCheck())
        {
            CustomPanel panel = (CustomPanel)target;
            panel.AdjustPanel();
        }

        if (GUILayout.Button("Adjust Panel"))
        {

            CustomPanel panel = (CustomPanel)target;
            panel.AdjustPanel();
        }
        if (GUILayout.Button("Add Button"))
        {

            CustomPanel panel = (CustomPanel)target;
            panel.AddButton();
        }
        if (GUILayout.Button("Remove Button"))
        {

            CustomPanel panel = (CustomPanel)target;
            panel.RemoveButton();
        }
    }*/
}
