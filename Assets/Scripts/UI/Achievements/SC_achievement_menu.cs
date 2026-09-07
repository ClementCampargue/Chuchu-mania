using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SC_achievement_menu : MonoBehaviour
{

    public InputActionReference quit;
    public static SC_achievement_menu instance;
    public List<SO_achievement> achievements;
    public int achievement_count;
    public SpriteRenderer item;
    public TextMeshPro text;
    public TextMeshPro pourcentage;
    public Animator item_box;
    public GameObject question_mark;
    public Material default_mat;
    public Material locked;
    public List<GameObject> wishes;
    public int current_wishes;
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        achievement_count = achievements.Count;
        quit.action.Enable();
        calculate_pourcentage();
    }

    private void UpdateWishes()
    {
        for (int i = 0; i < wishes.Count; i++)
        {
            wishes[i].SetActive(i < current_wishes);
        }
    }

    public void use_wish()
    {
        if (current_wishes > 0)
        {
            current_wishes--;
            UpdateWishes();
        }
    }

    void calculate_pourcentage()
    {
        int réussis = 0;

        foreach (SO_achievement achievement in achievements)
        {
            if (achievement.réussi)
            {
                réussis++;
            }
        }

        float pourcentageRéussi = (float)réussis / achievements.Count * 100f;
        pourcentage.text = pourcentageRéussi.ToString("000") + "%";
    }

    // Update is called once per frame
    void Update()
    {
        if (quit.action.WasPerformedThisFrame())
        {
            SC_screenshot_transition.instance.Capture("HUB");
        }
        UpdateCursorMode();
    }

    public void hide_sticker()
    {
        item_box.SetTrigger("hide");
    }

    public void show_sticker()
    {
        item_box.SetTrigger("show");
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

    public void update_display(SO_achievement achievement, SC_star_achievement star)
    {
        item.sprite = achievement.sticker_reward.sticker_sprite;

        if (star.red)
        {
            text.text = "???";
        }
        else
        {
            text.text = achievement.requirement;
        }

        if (!star.debloque)
        {
            question_mark.SetActive(true);

            item.material = locked;
        }
        else
        {
            question_mark.SetActive(false);

            if (achievement.sticker_reward.special_mat != null)
            {
                item.material = achievement.sticker_reward.special_mat;
            }
            else
            {
                item.material = default_mat;
            }
        }
    }



}
