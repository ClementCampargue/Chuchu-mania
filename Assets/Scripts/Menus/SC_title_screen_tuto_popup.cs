using UnityEngine;
using UnityEngine.EventSystems;

public class SC_title_screen_tuto_popup : MonoBehaviour
{
    public string tuto;
    public string levels;
    public void start_tuto()
    {
        SC_screenshot_transition.instance.Capture(tuto);
    }

    public void close_popup()
    {
        SC_SaveManager.instance.data.tutorialCompleted = true;
        SC_SaveManager.instance.SaveGame();
        SC_screenshot_transition.instance.Capture(levels);
    }

    private void Update()
    {
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
