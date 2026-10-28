using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.U2D;

public class SC_settings : MonoBehaviour
{
    public Animator anim;
    public List<GameObject> audio;
    public List< GameObject> display;
    public List<GameObject> game;
    public List<GameObject> controls;
    public List<Animator> anims;
    private string nextpage;

    public SpriteRenderer spr;
    public SpriteRenderer spr2;

    public Color audio_color;
    public Color display_color;
    public Color game_color;
    public Color controls_color;

    public Color audio_color2;
    public Color display_color2;
    public Color game_color2;
    public Color controls_color2;

    public float speed;
    public int page;

    public InputActionReference previous;
    public InputActionReference next;

    private bool caninput = true;
    public static SC_settings instance;
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        checkpage();

    }

    // Update is called once per frame
    void Update()
    {
        UpdateCursorMode();
        if (previous.action.WasPressedThisFrame() && caninput)
        {
            if (page > 0)
            {
                page--;
            }
            else
            {
                page = 3;
            }
            checkpage();
        }
        if (next.action.WasPressedThisFrame() && caninput)
        {
            if (page < 3)
            {
                page++;
            }
            else
            {
                page = 0;
            }
                checkpage();
        }
    }

    void checkpage()
    {
        caninput = false;
        if (page == 0)
        {
            change_page("game");
        }

        else if (page == 1)
        {
            change_page("display");
        }

        else if (page == 2)
        {
            change_page("audio");
        }

        else if (page == 3)
        {
            change_page("controls");
        }
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

    public void accept_input()
    {
        caninput = true;

    }

    public void dont_accept_input()
    {
        caninput = false;

    }
    public void change_page(string page)
    {

        if ( page != nextpage)
        {
            nextpage = page;
            anim.SetTrigger("change");
        }
        caninput = false; 
    }

    public void upfaate_visuals()
    {
        if (nextpage == "audio")
        {
            page = 2;
            StartCoroutine(LerpColor(spr, audio_color, speed));
            StartCoroutine(LerpColor(spr2, audio_color2, speed));

            foreach (GameObject go in audio) 
            { 
                go.SetActive(true);
            }

            foreach (Animator anim in anims) 
            {
                anim.SetBool("on", false);
            }
            foreach (GameObject go in controls) 
            { 
                go.SetActive(false);
            }
            foreach (GameObject go in game) 
            { 
                go.SetActive(false);
            }
            foreach (GameObject go in display) 
            { 
                go.SetActive(false);
            }
        }
        else if (nextpage == "controls")
        {
            page = 3;

            StartCoroutine(LerpColor(spr, controls_color, speed));
            StartCoroutine(LerpColor(spr2, controls_color2, speed));

            foreach (GameObject go in audio)
            {
                go.SetActive(false);
            }
            foreach (GameObject go in controls)
            {
                go.SetActive(true);
            }
            foreach (GameObject go in game)
            {
                go.SetActive(false);
            }
            foreach (GameObject go in display)
            {
                go.SetActive(false);
            }
        }
        else if (nextpage == "game")
        {
            page = 0;

            StartCoroutine(LerpColor(spr, game_color, speed));
            StartCoroutine(LerpColor(spr2, game_color2, speed));

            foreach (GameObject go in audio)
            {
                go.SetActive(false);
            }
            foreach (GameObject go in controls)
            {
                go.SetActive(false);
            }
            foreach (GameObject go in game)
            {
                go.SetActive(true);
            }
            foreach (GameObject go in display)
            {
                go.SetActive(false);
            }
        }
        else if (nextpage == "display")
        {
            page = 1;

            StartCoroutine(LerpColor(spr, display_color, speed));
            StartCoroutine(LerpColor(spr2, display_color2, speed));

            foreach (GameObject go in audio)
            {
                go.SetActive(false);
            }
            foreach (GameObject go in controls)
            {
                go.SetActive(false);
            }
            foreach (GameObject go in game)
            {
                go.SetActive(false);
            }
            foreach (GameObject go in display)
            {
                go.SetActive(true);
            }
        }
        foreach (Animator anim in anims)
        {
            anim.SetBool("on", false);
        }
        anims[page].SetBool("on", true);


    }
    public IEnumerator LerpColor(SpriteRenderer sprite, Color targetColor, float duration)
    {
        Color startColor = sprite.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            sprite.color = Color.Lerp(startColor, targetColor, elapsed / duration);
            yield return null;
        }

        sprite.color = targetColor;
    }
}
