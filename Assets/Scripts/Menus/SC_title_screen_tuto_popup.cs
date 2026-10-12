using UnityEngine;

public class SC_title_screen_tuto_popup : MonoBehaviour
{
    public string tuto;
    public Animator anim;
    void Start()
    {
        if(PlayerPrefs.GetInt("firstplay") == 0)
        {
            anim.enabled = true;
            PlayerPrefs.SetInt("firstplay", 1);
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void start_tuto()
    {
        SC_screenshot_transition.instance.Capture(tuto);
    }

    public void close_popup()
    {
        anim.SetTrigger("title_screen");
    }
}
