using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_world", menuName = "Scriptable Objects/SO_world")]
public class SO_world : ScriptableObject
{
    public List<SO_level> levels;

    public Sprite header_background;
    public Sprite planet_sprite;
    public List<Sprite> spirits;
    public List<bool> spirits_unlocked;

    public string name_;

    public int rank;

    public int price;
    public bool unlocked;
}
