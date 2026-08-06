using UnityEngine;

public class SC_cutscene : MonoBehaviour
{
    private SC_screenshot_transition transition;
    private string next_scene;
    public int next_level;
    public SC_dialogue_system dialogue;
    void Start()
    {
        next_level = PlayerPrefs.GetInt("Level");
        PlayerPrefs.SetInt("Level", next_level+1);
        if(next_level == 0)
        {
            next_scene = "ART";

        }
        else if (next_level == 1)
        {
            next_scene = "Ruche";
        }
        else if (next_level == 2)
        {
            next_scene = "Moon";
        }
        else if (next_level == 3)
        {
            next_scene = "MoneyScene";
        }
        transition = SC_screenshot_transition.instance;
        dialogue.enabled = false;
        Invoke("delay", 0.1f);
    }

    void delay()
    {
        dialogue.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void EndCutscene()
    {
        transition.Capture(next_scene);
    }
}
