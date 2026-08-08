using UnityEngine;

public class SC_hub : MonoBehaviour
{
    void Start()
    {
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
