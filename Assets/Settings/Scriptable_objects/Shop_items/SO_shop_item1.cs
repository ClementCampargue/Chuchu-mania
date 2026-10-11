using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "SO_shop_item1", menuName = "Scriptable Objects/SO_shop_item1")]
public class SO_shop_item1 : ScriptableObject
{
    public Sprite item_sprite;
    public LocalizedString item_name;
    public LocalizedString item_dialogue;
    public int Price;
}
