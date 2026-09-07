using UnityEngine;

[CreateAssetMenu(fileName = "SO_achievement", menuName = "Scriptable Objects/SO_achievement")]
public class SO_achievement : ScriptableObject
{
    public string requirement;
    public SO_Sticker sticker_reward;
    public bool réussi;
}
