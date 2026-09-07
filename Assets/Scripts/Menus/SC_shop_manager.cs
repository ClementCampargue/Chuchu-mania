using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;

public class SC_shop_manager : MonoBehaviour
{
    public SC_typewriter typewriter;
    public LocalizedString default_dialogue;
    public LocalizedString cant_buy_;
    public LocalizedString thanks;
    public List<LocalizedString> random_lines;
    public List<SC_Button> buttons;
    public SO_sticker_list stickers;
    public Animator anim;

    public SC_shop_button sticker_1;
    public SC_shop_button sticker_2;

    public bool talking = true;
    public InputActionReference submit;
    public InputActionReference quit;
    public InputActionReference talk;
    public SC_gachapon gacha;
    public GameObject buttons_;
    public static SC_shop_manager instance;
    public SC_menu_navigation menu;

    public bool cantalk;
    public bool canquit;
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        choose_stickers();
        quit.action.Enable();
        talk.action.Enable();
        submit.action.Enable();
    }
    void choose_stickers()
    {
        int index1 = Random.Range(0, stickers.stickers.Count);

        int index2;
        do
        {
            index2 = Random.Range(0, stickers.stickers.Count);
        }
        while (index2 == index1);

        sticker_1.sticker = stickers.stickers[index1];
        sticker_2.sticker = stickers.stickers[index2];
    }
    // Update is called once per frame
    void Update()
    {

        if (cantalk && talk.action.WasPressedThisFrame())
        {
            Talk();
        }
        if (typewriter.finished && talking)
        {
            typewriter.SetWaitInputVisible(true);
            if (submit.action.WasPerformedThisFrame())
            {
                show_menu();
            }
        }
        if (!typewriter.finished && talking && submit.action.WasPerformedThisFrame())
        {
            typewriter.FinishText();
            typewriter.SetWaitInputVisible(true);
        }

        if (quit.action.WasPerformedThisFrame() && canquit)
        {
            SC_screenshot_transition.instance.Capture("HUB");
        }
        UpdateCursorMode();
    }
    public void show_text_start()
    {
        typewriter.TriggerText(default_dialogue.GetLocalizedString());
    }
    public void show_text(string text)
    {if (talking) return;
        typewriter.TriggerText(text);

    }

  
    public void show_menu()
    {
        canquit = true;
        cantalk = true;
        typewriter.SetWaitInputVisible(false);

        buttons_.SetActive(true);
        menu.SelectFirstAvailable();
        anim.ResetTrigger("talk");
        anim.SetTrigger("Show_menu");
        talking = false;
    }


    public void Show_gacha()
    {
        cantalk = false;
        canquit = false;

        gacha.open_gacha();
        anim.ResetTrigger("Show_menu");
        anim.SetTrigger("Show_gacha");
        talking = false;
    }

    public void Talk()
    {
        SC_Button currentButton = menu.GetCurrentButton();

        if (currentButton != null)
            currentButton.UnSelect();

        cantalk = false;
        canquit = false;
        talking = true;

        anim.ResetTrigger("Show_menu");
        anim.SetTrigger("talk");

        buttons_.SetActive(false);

        string random =
            random_lines[Random.Range(0, random_lines.Count)]
            .GetLocalizedString();

        typewriter.TriggerText(random);
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
