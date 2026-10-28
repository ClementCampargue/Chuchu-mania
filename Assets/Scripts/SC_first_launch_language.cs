using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SC_first_launch_language : MonoBehaviour
{
    public string title;
    public string levels;
    public InputActionReference actioen;
    public SC_language_settings language;
    public SC_Button button;
    private void Start()
    {
        PlayerPrefs.SetFloat("Language", 1f);
        language = SC_language_settings.instance;
        language.Update_settings();
    }
    public void updatelanguage()
    {
        language.Update_settings();
    }

    public void Update()
    {
        language.Update_settings();
        if (actioen.action.WasPerformedThisFrame())
        {
            SC_screenshot_transition.instance.Capture(title);
        }
        UpdateCursorMode();
    }
    private void UpdateCursorMode()
    {
        if (SC_controller_manager.instance == null)
            return;

        bool usingController =
            SC_controller_manager.instance.using_controller;


        if (usingController)
        {
            if (SC_scursorManager.instance != null)
            {
                SC_scursorManager.instance.disable_cursor();
            }


            if (EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == null)
            {
                button.Select();
            }
        }
        else
        {
            if (SC_scursorManager.instance != null)
            {
                SC_scursorManager.instance.enable_cursor();
            }
        }
    }


}
