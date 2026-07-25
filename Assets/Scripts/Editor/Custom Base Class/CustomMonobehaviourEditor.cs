using UnityEditor;
using UnityEngine;
using Utility;

[CustomEditor(typeof(CustomMonoBehaviour), true)]
public class CustomMonobehaviourEditor : Editor
{
    protected override void OnHeaderGUI()
    {
        if (target == null) return;
        base.OnHeaderGUI();
    }

    public override void OnInspectorGUI()
    {
        if (target == null) return;

        serializedObject.Update();

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.ObjectField(
            "Script",
            MonoScript.FromMonoBehaviour((MonoBehaviour)target),
            typeof(MonoScript),
            false
        );
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(8);

        DrawPropertiesExcluding(serializedObject, "m_Script", "_debug");

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_debug"));

        serializedObject.ApplyModifiedProperties();
    }
}