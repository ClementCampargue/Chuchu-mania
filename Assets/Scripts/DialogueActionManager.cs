using UnityEngine;

public class DialogueActionManager : MonoBehaviour
{
    public static DialogueActionManager instance;

    

    private void Awake()
    {
        instance = this;
    }


    public void Execute(string actionID)
    {
        switch (actionID)
        {
            case "LOAD_SCENE":
                Load_scene();
                break;

            case "LOAD_STICKERS":
                Load_stickers();
                break;

            case "LOAD_GAME":
                Load_game();
                break;

            case "LOAD_SHOP":
                Load_shop();
                break;

            case "LOAD_ACHIEVEMENTS":
                Load_Achievements();
                break;

            case "CUICUI_ACHIEVEMENT":
                Load_Achievements();
                break;
        }
    }


    public void Load_stickers()
    {
        SC_screenshot_transition.instance.Capture("Stickers");
        SC_player.instance.rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        SC_player.instance.enabled = true;
        SC_player.instance.canMove = true;
    }

    public void Load_shop()
    {
        SC_screenshot_transition.instance.Capture("Shop");
        SC_player.instance.rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        SC_player.instance.enabled = true;
        SC_player.instance.canMove = true;
    }

    public void Load_Achievements()
    {
        SC_screenshot_transition.instance.Capture("Achievements");
        SC_player.instance.rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        SC_player.instance.enabled = true;
        SC_player.instance.canMove = true;
    }

    public void Load_scene()
    {
    }

    public void Load_game()
    {
        if (!SC_SaveManager.instance.data.tutorialCompleted)
        {
            SC_screenshot_transition.instance.Capture("Tuto_ask");
        }
        else
        {
            SC_screenshot_transition.instance.Capture("LEVEL_SELECTION");
        }
        SC_player.instance.rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        SC_player.instance.enabled = true;
        SC_player.instance.canMove = true;
    }

    public void cuicui_full_story()
    {
        SC_SaveManager.instance.AddAchievementProgress(AchievementType.cuicui_story, 1);

    }
    private void GiveItem()
    {
        Debug.Log("Objet donné");
    }


    private void StartFight()
    {
        Debug.Log("Combat commencé");
    }

}