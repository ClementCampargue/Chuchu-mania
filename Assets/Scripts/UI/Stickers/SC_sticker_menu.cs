
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SC_sticker_menu : MonoBehaviour
{
    public static SC_sticker_menu instance;

    private SC_sticker_UI currentDraggingSticker;
    private Material unlocked;
    private SC_SaveManager saveManager;

    [Header("References")]
    public Animator anim;

    [Header("Sticker Info")]
    public TextMeshPro sticker_name;
    public TextMeshPro sticker_description;
    public TextMeshPro artist;
    public TextMeshPro number_of_stickers;

    public SpriteRenderer sprite_image;

    public List<GameObject> stars;

    [Header("Mode")]
    public bool editing;
    public bool editing_controller;

    [Header("Input")]
    public InputActionReference move;
    public InputActionReference quit;
    public InputActionReference edit;
    public InputActionReference editend;

    [Header("Materials")]
    public Material not_unlocked;

    public GameObject infos;
    public SC_menu_navigation nav;
    private void Awake()
    {
        instance = this;
        saveManager = SC_SaveManager.instance;
    }

    private void Start()
    {
        if (sprite_image != null)
            unlocked = sprite_image.sharedMaterial;

        EnableAction(quit);
        EnableAction(edit);
        EnableAction(editend);
    }

    private void OnEnable()
    {
        EnableAction(edit);
        EnableAction(editend);
        EnableAction(quit);
    }

    private void OnDisable()
    {
        DisableAction(edit);
        DisableAction(editend);
        DisableAction(quit);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void EnableAction(InputActionReference actionReference)
    {
        if (actionReference != null &&
            actionReference.action != null)
        {
            actionReference.action.Enable();
        }
    }

    private void DisableAction(InputActionReference actionReference)
    {
        if (actionReference != null &&
            actionReference.action != null)
        {
            actionReference.action.Disable();
        }
    }

    public void SetDraggingSticker(SC_sticker_UI sticker)
    {
        currentDraggingSticker = sticker;
    }

    public void ClearDraggingSticker(SC_sticker_UI sticker)
    {
        if (currentDraggingSticker == sticker)
            currentDraggingSticker = null;
    }

    private void Update()
    {
        EnableAction(move);

        if (number_of_stickers != null &&
            SC_StickerSaveSystem.instance != null)
        {
            number_of_stickers.text =
                SC_StickerSaveSystem.instance.stickerCount.ToString("D2")
                + "/50";
        }

        // QUITTER LE MENU
        if (quit != null &&
            quit.action.WasPressedThisFrame() &&
            !editing)
        {
            if (SC_scursorManager.instance == null ||
                !SC_scursorManager.instance.grabing)
            {
                if (SC_screenshot_transition.instance != null)
                    SC_screenshot_transition.instance.Capture("HUB");
            }
        }

        // ENTRER EN EDIT MODE
        if (edit != null &&
            edit.action.WasReleasedThisFrame() &&
            !editing)
        {
            SC_scursorManager.instance.SetNormalCursor();
            editing_controller = true;
            SC_scursorManager.instance.RecenterCursor();
            StartEditMode();
        }
        else if (editend != null &&
                 editend.action.WasReleasedThisFrame() &&
                 editing)
        {
            editing_controller = false;
            QuitEditMode();
        }

        // GESTION DU CURSEUR
        if (SC_scursorManager.instance != null)
        {
            SC_scursorManager.instance.UpdateStickerCursor(editing);
        }
    }

    // =========================================
    // EDIT MODE
    // =========================================

    public void StartEditMode()
    {
        if (editing)
            return;
        nav.enabled = false;
        editing = true;

        if (SC_scursorManager.instance != null)
            SC_scursorManager.instance.enable_cursor();
        if (SC_scursorManager.instance != null)
            SC_scursorManager.instance.SetNormalCursor();

        if (anim != null)
            anim.SetTrigger("On");
    }

    public void start_edit_mode()
    {
        StartEditMode();
    }

    public void QuitEditMode()
    {
        if (currentDraggingSticker != null)
        {
            SC_sticker_UI sticker = currentDraggingSticker;
            currentDraggingSticker = null;

            sticker.DeleteIfDragging();
        }
        nav.enabled = true;

        editing = false;

        if (anim != null)
            anim.SetTrigger("Off");

        if (SC_scursorManager.instance != null)
            SC_scursorManager.instance.UpdateCursorMode();
            SC_scursorManager.instance.RecenterCursor();
    }

    public void quit_edit_mode()
    {
        QuitEditMode();
    }

    // =========================================
    // STICKER INFO
    // =========================================

    private bool IsStickerUnlocked(SO_Sticker sticker)
    {
        if (sticker == null)
            return false;

        if (saveManager == null)
            saveManager = SC_SaveManager.instance;

        if (saveManager == null)
            return false;

        return saveManager.IsStickerUnlocked(sticker);
    }

    public void update_infos(SO_Sticker sticker, SC_Button but)
    {
        if (sticker == null)
            return;

        bool isUnlocked = IsStickerUnlocked(sticker);

        if (sprite_image != null)
            sprite_image.sprite = sticker.sticker_sprite;

        if (artist != null)
            artist.text = sticker.artist;

        UpdateStars(sticker.rarity);

        if (isUnlocked)
        {
            if (infos != null)
                infos.SetActive(true);

            if (sticker_name != null)
                sticker_name.text =
                    sticker.sticker_name.GetLocalizedString();

            if (sticker_description != null)
                sticker_description.text =
                    sticker.description.GetLocalizedString();

            if (sprite_image != null)
            {
                sprite_image.sharedMaterial =
                    sticker.special_mat != null
                        ? sticker.special_mat
                        : unlocked;
            }
        }
        else
        {
            if (infos != null)
                infos.SetActive(false);

            if (sticker_name != null)
                sticker_name.text = "???";

            if (sticker_description != null)
            {
                sticker_description.text =
                    sticker.unlock_conditions.GetLocalizedString();
            }

            if (sprite_image != null)
                sprite_image.sharedMaterial = not_unlocked;
        }
    }

    // =========================================
    // STARS
    // =========================================

    public void UpdateStars(int rarity)
    {
        if (stars == null)
            return;

        foreach (GameObject star in stars)
        {
            if (star != null)
                star.SetActive(false);
        }

        rarity = Mathf.Clamp(rarity, 0, stars.Count);

        for (int i = 0; i < rarity; i++)
        {
            if (stars[i] != null)
                stars[i].SetActive(true);
        }
    }
}
