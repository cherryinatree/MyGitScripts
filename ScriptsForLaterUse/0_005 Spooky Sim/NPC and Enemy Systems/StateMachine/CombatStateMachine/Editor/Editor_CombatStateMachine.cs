using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(CombatStateMachine), true)]
public class Editor_CombatStateMachineEditor : Editor
{
    protected CSM_RecordableList _list;
    protected SerializedProperty _resetBrainOnEnable;
    protected SerializedProperty _resetBrainOnStart;

    protected virtual void OnEnable()
    {
        _list = new CSM_RecordableList(serializedObject.FindProperty("States"));
        _list.elementNameProperty = "States";
        _list.elementDisplayType = CSM_RecordableList.ElementDisplayType.Expandable;

        _resetBrainOnEnable = serializedObject.FindProperty("ResetBrainOnEnable");
        _resetBrainOnStart = serializedObject.FindProperty("ResetBrainOnStart");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Optional: draw script field read-only
        using (new EditorGUI.DisabledScope(true))
        {
            SerializedProperty scriptProp = serializedObject.FindProperty("m_Script");
            if (scriptProp != null)
            {
                EditorGUILayout.PropertyField(scriptProp);
            }
        }

        EditorGUILayout.Space();

        // Your special state list UI
        _list.DoLayoutList();

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_resetBrainOnEnable);
        EditorGUILayout.PropertyField(_resetBrainOnStart);

        EditorGUILayout.Space();

        // Draw all remaining properties from derived classes
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "States",
            "ResetBrainOnEnable",
            "ResetBrainOnStart"
        );

        serializedObject.ApplyModifiedProperties();

        CombatStateMachine brain = (CombatStateMachine)target;
        if (brain.CurrentState != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current State", brain.CurrentState.StateName);
        }
    }
}