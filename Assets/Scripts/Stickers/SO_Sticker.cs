using UnityEngine;

[CreateAssetMenu(fileName = "SO_Sticker", menuName = "Scriptable Objects/SO_Sticker")]
public class SO_Sticker : ScriptableObject
{
    public Sprite sticker_sprite;
    public string sticker_name;
    [TextArea(3, 3)]
    public string description;
    public string artist;
    public int rarity;
    public int Price;
    public bool unlocked;
}
