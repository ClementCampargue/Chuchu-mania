
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SO_world",
    menuName = "Scriptable Objects/SO_world"
)]
public class SO_world : ScriptableObject
{
    [Header("Identité")]
    [Tooltip("Identifiant unique et permanent, ex : forest")]
    public string worldId;

    [Header("Niveaux")]
    public List<SO_level> levels = new List<SO_level>();

    [Header("Visuels")]
    public Sprite header_background;
    public Sprite planet_sprite;
    public List<Sprite> spirits = new List<Sprite>();

    [Header("Progression initiale")]
    public bool startUnlocked;

    public List<bool> defaultSpiritsUnlocked =
        new List<bool>();

    public string name_;
    public int price;
}
