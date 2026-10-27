
using System.Collections.Generic;
using UnityEngine;

public class SC_display_settings : MonoBehaviour
{
    public SC_pause pause;
    public GameObject overlay_;
    [System.Serializable]
    public class ResolutionOption
    {
        public int width = 1920;
        public int height = 1080;
    }

    [Header("Résolutions disponibles")]
    public List<ResolutionOption> resolutions = new List<ResolutionOption>()
    {
        new ResolutionOption { width = 1920, height = 1080 },
        new ResolutionOption { width = 1600, height = 900 },
        new ResolutionOption { width = 1280, height = 720 },
        new ResolutionOption { width = 1024, height = 768 }
    };

    private int lastResolution = -1;
    private int lastScreen = -1;

    void Start()
    {
        update_settings();
    }

    public void update_settings()
    {
        int resolutionIndex = (int)PlayerPrefs.GetFloat("Resolution", 0f);
        int screen = (int)PlayerPrefs.GetFloat("Ecran", 0f);
        int VSYNC = (int)PlayerPrefs.GetFloat("V sync", 0f);
        int overlay = (int)PlayerPrefs.GetFloat("Overlay", 0f);
        int fps = (int)PlayerPrefs.GetFloat("FPS", 0f);

   
        switch (VSYNC)
        {
            case 0:
                QualitySettings.vSyncCount = 1;
                break;

            case 1:
                QualitySettings.vSyncCount = 0;
                break;


        }
        switch (overlay)
        {
            case 0:
                overlay_.SetActive(false);
                break;

            case 1:
                overlay_.SetActive(true);
                break;


        }
        switch (fps)
        {
            case 0:
              Application.targetFrameRate = 30;
                break;

            case 1:
                Application.targetFrameRate = 60;
                break;

            case 2:
                Application.targetFrameRate = 120;
                break;

            case 3:
                Application.targetFrameRate = 240;
                break;


        }
        // Sécurise l'index de résolution
        if (resolutions == null || resolutions.Count == 0)
            return;

        resolutionIndex = Mathf.Clamp(
            resolutionIndex, 0, resolutions.Count - 1
        );

        // Rien n'a changé : aucune modification nécessaire
        if (resolutionIndex == lastResolution && screen == lastScreen)
            return;

        ResolutionOption resolution = resolutions[resolutionIndex];

        // Détermine le mode d'affichage
        FullScreenMode mode;

        switch (screen)
        {
            case 0:
                mode = FullScreenMode.ExclusiveFullScreen;
                break;

            case 1:
                mode = FullScreenMode.FullScreenWindow;
                break;

            case 2:
                mode = FullScreenMode.Windowed;
                break;

            default:
                mode = FullScreenMode.ExclusiveFullScreen;
                screen = 0;
                break;
        }



        // Applique uniquement les nouveaux paramètres
        Screen.fullScreenMode = mode;

        Screen.SetResolution(
            resolution.width,
            resolution.height,
            mode
        );



        // Mémorise les paramètres appliqués
        lastResolution = resolutionIndex;
        lastScreen = screen;
    }

    private void Update()
    {
        if (pause != null && pause.isPaused)
        {
            update_settings();
        }
    }
}
