using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SO_Dialogue))]
public class SO_DialogueEditor : Editor
{
    SerializedProperty groups;

    void OnEnable()
    {
        groups = serializedObject.FindProperty("groups");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        for (int i = 0; i < groups.arraySize; i++)
        {
            SerializedProperty group = groups.GetArrayElementAtIndex(i);

            SerializedProperty lines = group.FindPropertyRelative("lines");
            SerializedProperty hasChoice = group.FindPropertyRelative("hasChoice");
            SerializedProperty choice1 = group.FindPropertyRelative("choice1");
            SerializedProperty choice2 = group.FindPropertyRelative("choice2");

            EditorGUILayout.PropertyField(lines, true);

            EditorGUILayout.PropertyField(hasChoice);

            if (hasChoice.boolValue)
            {
                EditorGUILayout.PropertyField(choice1, true);
                EditorGUILayout.PropertyField(choice2, true);
            }

            EditorGUILayout.Space();
        }

        serializedObject.ApplyModifiedProperties();
    }
}