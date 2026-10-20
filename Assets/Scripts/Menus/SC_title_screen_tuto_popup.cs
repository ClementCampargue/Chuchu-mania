using UnityEngine;

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
        SC_screenshot_transition.instance.Capture(levels);
    }
}
