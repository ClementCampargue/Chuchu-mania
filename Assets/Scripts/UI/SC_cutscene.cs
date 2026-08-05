using UnityEngine;

public class SC_cutscene : MonoBehaviour
{
    public string next_scene;
    private SC_screenshot_transition transition;
    void Start()
    {
        transition = SC_screenshot_transition.instance;
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
