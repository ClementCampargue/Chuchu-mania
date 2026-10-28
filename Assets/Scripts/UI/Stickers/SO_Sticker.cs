
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(
    fileName = "SO_Sticker",
    menuName = "Scriptable Objects/SO_Sticker"
)]
public class SO_Sticker : ScriptableObject
{
    [Header("Identification")]
    public string stickerId;

    [Header("Visuel")]
    public Sprite sticker_sprite;
    public Material special_mat;

    [Header("Informations")]
    public LocalizedString sticker_name;
    public LocalizedString description;
    public LocalizedString unlock_conditions;

    [Header("Autres données")]
    public string artist;
    public int rarity;
    public int Price;
}
