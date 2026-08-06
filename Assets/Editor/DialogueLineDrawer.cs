using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DialogueLine))]
public class DialogueLineDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;

        height += EditorGUI.GetPropertyHeight(
            property.FindPropertyRelative("character")
        );

        height += EditorGUI.GetPropertyHeight(
            property.FindPropertyRelative("text")
        );

        height += EditorGUIUtility.singleLineHeight;

        bool hasChoices = property.FindPropertyRelative("hasChoices").boolValue;

        if (hasChoices)
        {
            height += EditorGUI.GetPropertyHeight(
                property.FindPropertyRelative("choices"),
                true
            );
        }

        return height;
    }


    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float y = position.y;


        SerializedProperty character =
            property.FindPropertyRelative("character");

        SerializedProperty text =
            property.FindPropertyRelative("text");

        SerializedProperty hasChoices =
            property.FindPropertyRelative("hasChoices");

        SerializedProperty choices =
            property.FindPropertyRelative("choices");


        EditorGUI.PropertyField(
            new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
            character
        );

        y += EditorGUI.GetPropertyHeight(character);


        EditorGUI.PropertyField(
            new Rect(position.x, y, position.width, EditorGUI.GetPropertyHeight(text)),
            text
        );

        y += EditorGUI.GetPropertyHeight(text);


        EditorGUI.PropertyField(
            new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
            hasChoices
        );

        y += EditorGUIUtility.singleLineHeight;


        if (hasChoices.boolValue)
        {
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width,
                EditorGUI.GetPropertyHeight(choices, true)),
                choices,
                true
            );
        }


        EditorGUI.EndProperty();
    }
}