using UnityEngine;
using UnityEngine.Audio;

public class SC_game_settings : MonoBehaviour
{
    public float rumble;
    public SC_pause pause;

    void Start()
    {
        update_settings();
    }

    public void update_settings()
    {
        rumble = PlayerPrefs.GetFloat("Vibration");
       
        
        if(rumble == 0)
        {
            SC_rumbleManager.instance.rumble_enabled = true;
        }
        else
        {
            SC_rumbleManager.instance.rumble_enabled = false;
        }


    }
    private void Update()
    {
        if (pause.isPaused)
        {
            update_settings();
        }

    }
}
