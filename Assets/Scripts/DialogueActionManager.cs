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
        }
    }


    private void Load_stickers()
    {
        SC_screenshot_transition.instance.Capture("Stickers");
        SC_player.instance.rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        SC_player.instance.enabled = true;
        SC_player.instance.canMove = true;
    }

    private void Load_scene()
    {
    }

    private void Load_game()
    {
        SC_screenshot_transition.instance.Capture("Cutscene1");
        SC_player.instance.rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        SC_player.instance.enabled = true;
        SC_player.instance.canMove = true;
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