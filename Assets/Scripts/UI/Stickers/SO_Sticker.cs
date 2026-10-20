using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(
    fileName = "SO_Sticker",
    menuName = "Scriptable Objects/SO_Sticker"
)]
public class SO_Sticker : ScriptableObject
{
    public string stickerId;

    public Sprite sticker_sprite;
    public LocalizedString sticker_name;
    public LocalizedString description;
    public LocalizedString unlock_conditions;

    public string artist;
    public int rarity;
    public int Price;

    public bool unlocked;

    public Material special_mat;
}