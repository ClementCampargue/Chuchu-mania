using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class SC_cutscene : MonoBehaviour
{
    private SC_screenshot_transition transition;
    public string next_scene;
    public DialogueManager dialogue;

    public DialogueData dialogue_to_play;
    public static SC_cutscene instance;
    public Animator anim;
    public InputActionReference skip;

    private bool canadvance;
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        anim.speed = 0;
        dialogue.currentDialogue = dialogue_to_play;
        dialogue.StartDialogue();
        Invoke("delay",1);
        transition = SC_screenshot_transition.instance;
    }

    void delay()
    {
        canadvance = true;
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
    {if (!canadvance) return;
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
