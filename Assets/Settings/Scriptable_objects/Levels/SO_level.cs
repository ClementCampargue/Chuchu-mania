using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_level", menuName = "Scriptable Objects/SO_level")]
public class SO_level : ScriptableObject
{
    public Sprite header_background;
    public Sprite planet_sprite;
    public List<string> levels;
    public List<Sprite> spirits;
    public List<bool> spirits_unlocked;

    public string name_;

    public int rank;

    public int price;
    public bool unlocked;
}
