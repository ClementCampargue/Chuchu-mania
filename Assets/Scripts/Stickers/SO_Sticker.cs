using UnityEngine;

[CreateAssetMenu(fileName = "SO_Sticker", menuName = "Scriptable Objects/SO_Sticker")]
public class SO_Sticker : ScriptableObject
{
    public Sprite sticker_sprite;
    public string sticker_name;
    public bool unlocked;
}
