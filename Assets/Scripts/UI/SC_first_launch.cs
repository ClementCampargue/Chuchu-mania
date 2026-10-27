using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Localization;

public class SC_first_launch : MonoBehaviour
{


    void OnEnable()
    {
        if(PlayerPrefs.GetFloat("firstlaunchever") == 0)
        {




            // AUDIO

            PlayerPrefs.SetFloat("Master", 1);
            PlayerPrefs.SetFloat("Music", 1);
            PlayerPrefs.SetFloat("Sound", 1);


            // GAME

            PlayerPrefs.SetFloat("Langue", 0);


            PlayerPrefs.SetFloat("Tremblement", 0);
            PlayerPrefs.SetFloat("Vibration", 0);
            PlayerPrefs.SetFloat("Cheats", 1);


            // DISPLAY

            PlayerPrefs.SetFloat("Resolution", 3);
            PlayerPrefs.SetFloat("Ecran", 0);
            PlayerPrefs.SetFloat("V sync", 0);
            PlayerPrefs.SetFloat("FPS", 2);
            PlayerPrefs.SetFloat("Overlay", 0);

            // CONTROLS



        PlayerPrefs.SetFloat("firstlaunchever", 1);

            PlayerPrefs.Save();
        }
    }


    private void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
