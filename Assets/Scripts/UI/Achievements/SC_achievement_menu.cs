
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SC_achievement_menu : MonoBehaviour
{
    public InputActionReference move;
    public InputActionReference quit;

    public static SC_achievement_menu instance;

    public List<SO_achievement> achievements;
    public int achievement_count;

    public SpriteRenderer item;
    public TextMeshPro text;
    public TextMeshPro pourcentage;
    public Animator item_box;

    public GameObject question_mark;
    public GameObject checkmark;

    public Material default_mat;
    public Material locked;

    [Header("Wishes")]
    public List<GameObject> wishes;
    public int current_wishes;

    [Header("Popup Use Wish")]
    public GameObject useWishPopup;

    public SC_star_achievement pendingStar;
    public bool caninteract = true;
    private void Awake()
    {
        instance = this;

        if (useWishPopup != null)
        {
            useWishPopup.SetActive(false);
        }
    }

    private void Start()
    {
        SC_scursorManager.instance.enable_cursor();
        if (move != null)
        {
            move.action.Enable();
        }

        if (quit != null)
        {
            quit.action.Enable();
        }

        achievement_count = achievements.Count;

        current_wishes = SC_SaveManager.instance.data.wishes;

        calculate_pourcentage();
        UpdateWishes();
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
        if (current_wishes <= 0)
            return;

        SC_SaveManager.instance.SpendWish(1);

        current_wishes--;

        UpdateWishes();
        calculate_pourcentage();
    }

    // Ouvre la popup sans consommer de wish.
    public void ShowWishPopup(SC_star_achievement star)
    {
        if (star == null || current_wishes <= 0)
            return;
        caninteract = false;
        hide_sticker();

        pendingStar = star;

        if (useWishPopup != null)
        {
            useWishPopup.SetActive(true);
        }
        else
        {
            Debug.LogWarning("La popup Use Wish n'est pas assignée.");
            star.anim.SetTrigger("Unhover");
        }
    }

    // Bouton CONFIRMER de la popup.
    public void ConfirmWishPopup()
    {
        if (pendingStar == null)
        {
            CloseWishPopup();
            return;
        }
        SC_star_achievement star = pendingStar;

        CloseWishPopup();

        star.anim.SetTrigger("Press");
        star.ConfirmWishUnlock();
    }

    // Bouton ANNULER de la popup.
    public void CancelWishPopup()
    {
        show_sticker();
        SC_star_achievement star = pendingStar;
        star.anim.SetTrigger("Unhover");

        CloseWishPopup();
        Invoke("delay", 0.2f);
    }
    public void check_constellation()
    {
        SC_star_achievement star = pendingStar;
    star.constellation.CheckAllStarsUnlocked();
        star.anim.SetTrigger("Unhover");

    }
    void delay()
    {
        caninteract = true;

    }

    private void CloseWishPopup()
    {
        if (useWishPopup != null)
        {
            useWishPopup.SetActive(false);
        }
        SC_star_achievement star = SC_achievement_menu.instance.pendingStar;
        star.anim.SetTrigger("Unhover");

    }

    private void calculate_pourcentage()
    {
        int réussis = 0;

        foreach (SO_achievement achievement in achievements)
        {
            if (SC_SaveManager.instance != null &&
                SC_SaveManager.instance.IsAchievementUnlocked(achievement))
            {
                réussis++;
            }
        }

        float pourcentageRéussi = achievements.Count > 0
            ? (float)réussis / achievements.Count * 100f
            : 0f;

        pourcentage.text = pourcentageRéussi.ToString("000") + "%";
    }

    private void Update()
    {
        move.action.Enable();

        if (quit != null && quit.action.WasPerformedThisFrame())
        {
            if (caninteract)
            {
                SC_screenshot_transition.instance.Capture("HUB");
            }
            else
            {
                CancelWishPopup();
            }
        }

        UpdateCursorMode();

        if (Input.GetKeyDown(KeyCode.W))
        {
            SC_SaveManager.instance.AddWish(1);

            current_wishes = SC_SaveManager.instance.data.wishes;

            UpdateWishes();
        }
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

    public void update_display(
        SO_achievement achievement,
        SC_star_achievement star)
    {
        item.sprite = achievement.sticker_reward.sticker_sprite;

        if (star.red)
        {
            text.text = "???";
        }
        else
        {
            text.text = achievement.requirement.GetLocalizedString();
        }

        checkmark.SetActive(
            SC_SaveManager.instance.IsAchievementFinished(achievement)
        );

        if (!SC_SaveManager.instance.IsAchievementCollected(achievement))
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
