using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SocialPlatforms.Impl;

public class SC_hub : MonoBehaviour
{
    private SC_money_manager money;
    public TextMeshPro name_;
    public TextMeshPro money_text;

    public Transform shop;
    public Transform stickers;
    public Transform Achievements;
    public Transform ship;
    public Transform spawn_point;

    public GameObject achievement_waiting;


    public LocalizedString default_;
    public LocalizedString shop_;
    public LocalizedString ship_;
    public LocalizedString stickers_;
    public LocalizedString constellation_;
    private void Awake()
    {
        if (SC_game_master.instance.previousScene == "LEVEL_SELECTION")
        {
            spawn_point.position = ship.position;
        }
        if (SC_game_master.instance.previousScene == "Shop")
        {
            spawn_point.position = shop.position;
        }
        else if (SC_game_master.instance.previousScene == "Stickers")
        {
            spawn_point.position = stickers.position;
        }

        else if (SC_game_master.instance.previousScene == "Achievements")
        {
            spawn_point.position = Achievements.position;
        }
        achievement_waiting.SetActive(HasPendingAchievement());


    }
    void Start()
    {

        money = SC_money_manager.instance;
        PlayerPrefs.SetInt("Score", 0);
        PlayerPrefs.SetInt("Level",0);
        SC_player.instance.canMove = true;
        money_text.text = SC_SaveManager.instance.data.money.ToString("D6");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void scene_load()
    {
        SC_screenshot_transition.instance.Capture("Cutscene1");
        SC_player.instance.rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        SC_player.instance.enabled = true;
        SC_player.instance.canMove = true;
    }
    public bool HasPendingAchievement()
    {
        if (SC_SaveManager.instance == null)
            return false;

        SC_SaveData data = SC_SaveManager.instance.data;

        if (data == null || data.achievements == null)
            return false;

        foreach (SC_AchievementProgress progress in data.achievements)
        {
            if (progress == null)
                continue;

            if (progress.finished && !progress.collected)
                return true;
        }

        return false;
    }

    public void update_name(int i) 
    {

        if(i == 0)
        {
            name_.text = default_.GetLocalizedString();
        }
        else if(i == 1)
        {
            name_.text = shop_.GetLocalizedString();
        }
        else if(i == 2)
        {
            name_.text = ship_.GetLocalizedString();
        }
        else if(i == 3)
        {
            name_.text = stickers_.GetLocalizedString();
        }
        else if(i == 4)
        {
            name_.text = constellation_.GetLocalizedString();
        }
    }
}
