using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class SC_language_settings : MonoBehaviour
{public static SC_language_settings instance;
    public List<Locale> locales;
    public SC_pause pause;


    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        Update_settings();
    }

    public void Update_settings()
    {
        int language = Mathf.RoundToInt(PlayerPrefs.GetFloat("Language", 0f));

        Locale selectedLocale = locales[language];
        LocalizationSettings.SelectedLocale = selectedLocale;

        Debug.Log("Langue changée : " + selectedLocale.Identifier.Code);
    }

    private void Update()
    {
        if (pause.isPaused)
        {
            Update_settings();
        }

    }
}