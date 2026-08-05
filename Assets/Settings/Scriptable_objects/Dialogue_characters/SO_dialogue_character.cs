using UnityEngine;

[CreateAssetMenu(fileName = "SO_dialogue_character", menuName = "Scriptable Objects/SO_dialogue_character")]
public class SO_dialogue_character : ScriptableObject
{
    public Sprite portrait;
    public string name;
    public AudioClip typesound;
}
