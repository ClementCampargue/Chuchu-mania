using UnityEngine;

[CreateAssetMenu(fileName = "SO_shop_item1", menuName = "Scriptable Objects/SO_shop_item1")]
public class SO_shop_item1 : ScriptableObject
{
    public Sprite item_sprite;
    public string item_name;
    public string item_dialogue;
    public int Price;
}
