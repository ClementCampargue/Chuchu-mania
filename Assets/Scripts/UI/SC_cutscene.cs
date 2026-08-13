using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class SC_cutscene : MonoBehaviour
{
    private SC_screenshot_transition transition;
    public string next_scene;
    public int next_level;
    public DialogueManager dialogue;

    public DialogueData dialogue_to_play;
    public static SC_cutscene instance;
    public Animator anim;
    public InputActionReference skip;
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        dialogue.currentDialogue = dialogue_to_play;
        dialogue.StartDialogue();
        next_level = PlayerPrefs.GetInt("Level");
        PlayerPrefs.SetInt("Level", next_level+1);
        if(next_level == 0)
        {
            next_scene = "Beach";

        }
        else if (next_level == 1)
        {
            next_scene = "Ruche";
        }
        else if (next_level == 2)
        {
            next_scene = "Moon";
        }
        else if (next_level == 3)
        {
            next_scene = "MoneyScene";
        }
        transition = SC_screenshot_transition.instance;
    }



    // Update is called once per frame
    void Update()
    {
        if (skip.action.WasPerformedThisFrame())
        {
            EndCutscene();
            this.enabled = false;
        }
    }
    public void EndCutscene()
    {
        transition.Capture(next_scene);
    }
    public void ResumeCutscene()
    {
        anim.speed = 1;
    }
    public void PauseCutscene()
    {
        anim.speed = 0;
    }
    public void change_music(AudioClip audio)
    {
        SC_music_manager.instance.update_music(audio, true);
    }
}
