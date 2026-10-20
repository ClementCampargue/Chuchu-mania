using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SC_win_screen : MonoBehaviour
{

    public SC_screenshot_transition transition;
    public static SC_win_screen instance;
    public Animator anim;
    public InputActionReference confirm;
    private bool once;
    private bool canact;
    public AudioSource audioSource;
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
        audioSource.Play();
        anim.ResetTrigger("show");
        anim.SetTrigger("hide");
        if (SceneManager.GetActiveScene().name == "TUTO")
        {
            transition.Capture("HUB");
        }
        else if (SC_level_master.instance.next_scene != string.Empty)
        {
            transition.Capture(SC_level_master.instance.next_scene);

        }
        else
        {
            transition.Capture("MoneyScene");
        }
        canact = false;
        once = false;
    }

    public void canact_()
    {
        canact = true;
    }

    public void Start_screen()
    {
        if(SceneManager.GetActiveScene().name == "TUTO")
        {
            SC_SaveManager.instance.data.tutorialCompleted = true;
            SC_SaveManager.instance.SaveGame();
            anim.enabled = true;
            anim.SetTrigger("tuto");
        }
        else
        {
            anim.enabled = true;
            anim.SetTrigger("show");

        }

    }
}
