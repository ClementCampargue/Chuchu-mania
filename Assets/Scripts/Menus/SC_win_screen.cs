using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class SC_win_screen : MonoBehaviour
{

    public SC_screenshot_transition transition;
    public string nextscene;
    public static SC_win_screen instance;
    public Animator anim;
    public InputActionReference confirm;
    private bool once;
    private bool canact;
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
        if (confirm.action.WasPerformedThisFrame() && !once && canact)
        {
            once = true;
            transition_();
        }
    }

    public void transition_()
    {
        anim.ResetTrigger("show");
        anim.SetTrigger("hide");
        transition.Capture(nextscene);
        canact = false;
        once = false;
    }

    public void canact_()
    {
        canact = true;
    }

    public void Start_screen()
    {
        anim.enabled = true;
        anim.SetTrigger("show");

    }
}
