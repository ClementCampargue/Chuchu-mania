using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SC_level_selection_screen : MonoBehaviour
{
    public int spirits_count;
    public TextMeshPro world_price;
    public TextMeshPro current_saved;
    public TextMeshPro record_score;
    public TextMeshPro record_time;
    public int record_rank;


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
    public List<SC_level_button> buttons;

    public SpriteRenderer header_background;
    public GameObject header_locked;
    public GameObject header_nothing;

    [Header("Spirits")]
    public List<SpriteRenderer> spirits;
    public List<Sprite> ranks;
    public List<SpriteRenderer> hearts;
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

    public SO_world world;

    private string scene_toload;
    public GameObject record_;


    public void update_record(int y)
    {
     
        SC_SaveManager saveManager = SC_SaveManager.instance;

        SC_LevelRecord record =
            saveManager.GetLevelRecord(world.levels[y]);
     
        if (record != null)
        {
            record_.SetActive(true);


            foreach (SpriteRenderer spr in hearts)
            {
                spr.enabled = false;
            }
            for(int i = 0; i < record.bestHearts; i++)
            {
                hearts[i].enabled = true;
            }
            record_score.text = record.bestScore.ToString("D6");
            record_time.text = record.bestTime.ToString("D3");
        }
        else
        {
            record_.SetActive(false);

            foreach (SpriteRenderer spr in hearts)
            {
                spr.enabled = false;
            }
        }

    }

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
        UpdateProgression();
    }
    public void UpdateProgression()
    {
        int completed =
            SC_SaveManager.instance.GetCompletedLevelsCount();
        spirits_count = completed;
        current_saved.text = completed.ToString();
    }
    public void start_game(int i)
    {
        if (buttons[i].cant_open)
        {
            return;
        }
        scene_toload = world.levels[i].scene_to_load;
        anim.SetTrigger("load");
        this.enabled = false;
    }

    public void load_scene()
    {
        SC_screenshot_transition.instance.Capture(scene_toload);

    }


    public void open_planet(SO_world level)
    {
        SC_SaveManager saveManager = SC_SaveManager.instance;
        world_price.text = level.price.ToString();
        record_.SetActive(false);

        if (level == null)
            return;
        SC_game_master.instance.world = level;
        world = level;

        for (int i = 0; i < buttons.Count; i++)
        {
            if (i >= world.levels.Count)
            {
                buttons[i].gameObject.SetActive(false);
                continue;
            }

            SO_level levelData = world.levels[i];

            bool unlocked_ =
                SC_SaveManager.instance != null &&
                SC_SaveManager.instance.data != null
                    ? SC_SaveManager.instance.IsLevelUnlocked(levelData)
                    : levelData.startUnlocked;

            buttons[i].check_locked(unlocked_);

            if (unlocked_)
            {
                TextMeshPro label =
                    buttons[i].GetComponent<TextMeshPro>();

                if (label != null)
                    label.text = levelData.levelname;
                
            }
            else
            {
                TextMeshPro label =
                    buttons[i].GetComponent<TextMeshPro>();

                label.text = "???";
            }
        }


        bool unlocked = SC_SaveManager.instance != null
            ? SC_SaveManager.instance.IsWorldUnlocked(world)
            : world.startUnlocked;

        if (unlocked)
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
        for (int i = 0; i < world.levels.Count; i++)
        {
            SC_LevelRecord record =
                saveManager.GetLevelRecord(world.levels[i]);

            if (record != null)
            {
                buttons[i].rank.sprite = ranks[record.bestRank];
            }
            else
            {
                // Sprite par défaut si aucun record
                buttons[i].rank.sprite = ranks[0];
            }
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
        if (world == null)
            return;

        if (SC_SaveManager.instance == null ||
            SC_SaveManager.instance.data == null)
        {

            return;
        }

        if (boss)
        {
            SC_music_manager.instance.update_music(unlock_music_boss);
        }
        else
        {
            SC_music_manager.instance.update_music(unlock_music, false);
        }

        SC_SaveManager.instance.SetWorldUnlocked(world, true);

        price.SetActive(false);
        locked_menu.SetActive(false);
        new_menu.SetActive(true);

        spr.sprite = world.planet_sprite;
        update_header(world);
    }

    public void unlock_level()
    {
        if(spirits_count>= world.price)
        {
            anim.SetTrigger("unlock");
        }
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

        // Vérifier si le monde est débloqué dans la sauvegarde active
        bool worldUnlocked;

        if (SC_SaveManager.instance != null &&
            SC_SaveManager.instance.data != null)
        {
            worldUnlocked =
                SC_SaveManager.instance.IsWorldUnlocked(level);
        }
        else
        {
            worldUnlocked = level.startUnlocked;
        }

        // Afficher l'en-tête correspondant à l'état du monde
        if (header_nothing != null)
            header_nothing.SetActive(false);

        if (header_locked != null)
            header_locked.SetActive(!worldUnlocked);

        // Si le monde est verrouillé, ne pas afficher ses informations
        if (!worldUnlocked)
            return;

        // Arrière-plan
        if (header_background != null)
            header_background.sprite = level.header_background;

        // Nom du monde
        if (name_ != null)
            name_.text = level.name_;

        // Rang du monde
        // Rang du monde
        if (rank != null)
        {
            SC_SaveManager saveManager = SC_SaveManager.instance;

            float totalRank = 0f;
            int count = 0;

            for (int i = 0; i < level.levels.Count; i++)
            {
                SC_LevelRecord record =
                    saveManager.GetLevelRecord(level.levels[i]);

                if (record != null)
                {
                    totalRank += record.bestRank;
                    count++;
                }
            }

            float averageRank = count > 0 ? totalRank / count : 0f;

            rank.sprite = ranks[(int)averageRank];
        }

if (spirits == null || level == null)
            return;

        if (level.levels == null)
        {
            // Désactiver tous les esprits si la liste des niveaux est absente
            foreach (SpriteRenderer renderer in spirits)
            {
                if (renderer != null)
                    renderer.gameObject.SetActive(false);
            }

            return;
        }

        for (int i = 0; i < spirits.Count; i++)
        {
            SpriteRenderer spiritRenderer = spirits[i];

            if (spiritRenderer == null)
                continue;

            // Vérifier que l'esprit existe dans la liste
            bool hasSpirit = i < level.levels.Count &&
                             level.levels[i] != null;

            spiritRenderer.gameObject.SetActive(hasSpirit);

            if (!hasSpirit)
                continue;

            // Appliquer le sprite
            spiritRenderer.sprite = level.levels[i].spirit;

            bool isCompleted =

              SC_SaveManager.instance.IsLevelCompleted(level.levels[i]);

            Material targetMaterial = isCompleted
                ? unlocked_spirit
                : locked_spirit;

            spirits[i].material = targetMaterial;
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
