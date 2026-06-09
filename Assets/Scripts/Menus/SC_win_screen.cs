using UnityEngine;

public class SC_win_screen : MonoBehaviour
{

    public SC_screenshot_transition transition;
    public string nextscene;
    public static SC_win_screen instance;
    public Animator anim;
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void transition_()
    {
        anim.ResetTrigger("show");
        anim.SetTrigger("hide");
        transition.Capture(nextscene);
    }

    public void Start_screen()
    {
        anim.enabled = true;
        anim.SetTrigger("show");

    }
}
