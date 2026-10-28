
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(
    fileName = "SO_level",
    menuName = "Scriptable Objects/SO_level"
)]
public class SO_level : ScriptableObject
{
    [Header("Identity")]
    public string levelId;

    [Header("Level Info")]
    public LocalizedString levelname;
    public string scene_to_load;

    public Sprite spirit;
    [Header("Unlock")]
    public bool startUnlocked;
}
