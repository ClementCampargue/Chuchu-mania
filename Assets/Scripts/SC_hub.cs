using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class SC_hub : MonoBehaviour
{
    private SC_money_manager money;
    public TextMeshPro money_text;
    void Start()
    {
        money = SC_money_manager.instance;
        money_text.text = money.money.ToString("D6");
        PlayerPrefs.SetInt("Score", 0);
        PlayerPrefs.SetInt("Level",0);
        SC_player.instance.canMove = true;
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
}
