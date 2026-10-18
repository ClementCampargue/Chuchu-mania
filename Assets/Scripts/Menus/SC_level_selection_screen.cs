using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SC_level_selection_screen : MonoBehaviour
{
    public AudioClip default_music;
    public AudioClip unlock_music;
    public AudioClip unlock_music_boss;
    public AudioClip final_mode;

    public Sprite sprite_locked_planet_big;
    public SpriteRenderer spr;
    public bool selecting_level;
    public InputActionReference back;
    public SC_Button button;
    public Animator anim;
    public GameObject price;
    public List<SC_Button> buttons;

    public SpriteRenderer header_background;
    public GameObject header_locked;
    public GameObject header_nothing;

    [Header("Spirits")]
    public List<SpriteRenderer> spirits;
    public List<Sprite> ranks;
    public List<bool> spirits_unlocked;

    [Header("Level Info")]
    public TextMeshPro name_;
    public SpriteRenderer rank;

    [Header("Materials")]
    public Material locked_spirit;
    public Material unlocked_spirit;

    public static SC_level_selection_screen instance;
    public GameObject base_menu;
    public GameObject new_menu;
    public GameObject locked_menu;

    public AudioSource confirm;
    public AudioSource cancel;
    public bool boss;

    private SO_world world;
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        if (SC_scursorManager.instance != null)
        {
            SC_scursorManager.instance.enable_cursor();
        }
    }
    public void start_game(string scene)
    {
       SC_screenshot_transition.instance.Capture(scene);
    }

    public void open_planet(SO_world level)
    {


        world = level;
        
        for (int i = 0; i < buttons.Count; i++)
        {
            if (i >= world.levels.Count)
            {
                buttons[i].gameObject.SetActive(false);
            }
            else
            {
                buttons[i].gameObject.SetActive(true);
                buttons[i].GetComponent<TextMeshPro>().text = world.levels[i].levelname;
            }
        }
        if (level.unlocked)
        {
            spr.sprite = world.planet_sprite;
            base_menu.SetActive(true);
            locked_menu.SetActive(false);
            price.SetActive(false);
            new_menu.SetActive(false);
        }
        else
        {
            spr.sprite = sprite_locked_planet_big;

            price.SetActive(true);
            base_menu.SetActive(false);
            locked_menu.SetActive(true);
            new_menu.SetActive(false);
        }
        confirm.Play();
        anim.SetTrigger("open");
        selecting_level = true;
    }

    public void close_planet()
    {
        cancel.Play();
        selecting_level = false;
        anim.SetTrigger("close");
    }
    public void change_planet_unlocked()
    {
        if (boss)
        {
            SC_music_manager.instance.update_music(unlock_music_boss);
        }
        else
        {
            SC_music_manager.instance.update_music(unlock_music, false);
        }
        price.SetActive(false);
        locked_menu.SetActive(false);
        new_menu.SetActive(true);

        world.unlocked = true;
        spr.sprite = world.planet_sprite;
        update_header(world);
    }

    public void unlock_level()
    {

        anim.SetTrigger("unlock");
    }

    public void resume_music()
    {
        SC_music_manager.instance.update_music(default_music);
    }

    void Update()
    {
        if (back.action.WasPressedThisFrame())
        {
            if (selecting_level)
            {
                close_planet();
            }
            else
            {
                SC_screenshot_transition.instance.Capture("HUB");
            }
        }

        UpdateCursorMode();
    }

    public void close_header()
    {
        header_nothing.SetActive(true);
    }
    public void update_header(SO_world level)
    {

        if (level == null)
            return;
        header_nothing.SetActive(false);
        header_locked.SetActive(!level.unlocked);


        if (level.unlocked)
        {
            // Background
            if (header_background != null)
            {
                header_background.sprite = level.header_background;
            }

            // Name
            if (name_ != null)
            {
                name_.text = level.name_;
            }

            // Rank
            rank.sprite = ranks[level.rank];



            // Update spirits
            for (int i = 0; i < spirits.Count; i++)
            {
                SpriteRenderer spiritRenderer = spirits[i];

                if (spiritRenderer == null)
                    continue;

                // Hide spirit slots that aren't used by this level
                if (i >= level.spirits.Count)
                {
                    spiritRenderer.gameObject.SetActive(false);
                    continue;
                }

                // Show spirit slot
                spiritRenderer.gameObject.SetActive(true);

                // Assign sprite
                spiritRenderer.sprite = level.spirits[i];

                // Check unlock status
                bool isUnlocked =
                    level.spirits_unlocked != null &&
                    i < level.spirits_unlocked.Count &&
                    level.spirits_unlocked[i];

                // Assign material
                spiritRenderer.sharedMaterial = isUnlocked
                    ? unlocked_spirit
                    : locked_spirit;
            }
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

            if (EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == null)
            {
                SelectDefaultButton();
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

    private void SelectDefaultButton()
    {
        if (button == null)
            return;

        if (!button.gameObject.activeInHierarchy)
            return;

        button.Select();
    }
}
