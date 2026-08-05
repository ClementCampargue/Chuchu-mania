using UnityEngine;

[CreateAssetMenu(fileName = "SO_dialogue_line", menuName = "Scriptable Objects/SO_dialogue_line")]
public class SO_dialogue_line : ScriptableObject
{
    public SO_dialogue_character character;

    [TextArea(2, 5)]
    public string text;
}
