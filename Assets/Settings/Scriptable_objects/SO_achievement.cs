using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "SO_achievement", menuName = "Scriptable Objects/SO_achievement")]
public class SO_achievement : ScriptableObject
{
    public LocalizedString requirement;
    public SO_Sticker sticker_reward;
    public bool réussi;
    public AchievementType type;
    public int objectif = 1;
}
public enum AchievementType
{
    S_rank,
    No_damage_level,
    Loose_level1,
    Win1000coins,
    eat100cream,
    burn_enemy,
    A_rank_all_levels,
    moon_level_no_damage,
    level_no_damage,
    inflate_bee


}