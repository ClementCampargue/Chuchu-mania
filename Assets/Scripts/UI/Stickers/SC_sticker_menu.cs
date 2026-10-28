
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SC_sticker_menu : MonoBehaviour
{
    public static SC_sticker_menu instance;

    private SC_sticker_UI currentDraggingSticker;
    private Material unlocked;
    private SC_SaveManager saveManager;

    [Header("UI")]
    public Animator anim;

    public TextMeshProUGUI sticker_name;
    public TextMeshProUGUI sticker_description;
    public TextMeshProUGUI artist;
    public TextMeshProUGUI number_of_stickers;

    public Image sprite_image;

    public List<GameObject> stars;

    [Header("Mode")]
    public bool editing;

    [Header("Input")]
    public InputActionReference quit;
    public InputActionReference quit2;
    public InputActionReference edit;

    [Header("Materials")]
    public Material not_unlocked;

    [Header("Default Button")]
    public Button button;
    public GameObject infos;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        instance = this;
        saveManager = SC_SaveManager.instance;
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

    private void Start()
    {
        if (SC_controller_manager.instance != null)
            SC_controller_manager.instance.keyboardAsController = true;

        if (SC_scursorManager.instance != null)
        {
            SC_scursorManager.instance.gameObject.SetActive(false);
            SC_scursorManager.instance.enable_cursor();
        }

        Invoke(nameof(DelayCursor), 0.25f);

        if (sprite_image != null)
            unlocked = sprite_image.material;

        EnableAction(quit);
        EnableAction(quit2);
        EnableAction(edit);

        if (SC_scursorManager.instance != null)
            SC_scursorManager.instance.gameObject.SetActive(true);
    }

    private void EnableAction(InputActionReference actionReference)
    {
        if (actionReference != null && actionReference.action != null)
            actionReference.action.Enable();
    }

    private void DisableAction(InputActionReference actionReference)
    {
        if (actionReference != null && actionReference.action != null)
            actionReference.action.Disable();
    }

    private void DelayCursor()
    {
        if (SC_scursorManager.instance != null)
            SC_scursorManager.instance.gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        EnableAction(edit);
        EnableAction(quit);
        EnableAction(quit2);
    }

    private void OnDisable()
    {
        if (SC_scursorManager.instance != null)
            SC_scursorManager.instance.gameObject.SetActive(false);

        DisableAction(edit);
        DisableAction(quit);
        DisableAction(quit2);
    }

    private void OnDestroy()
    {
        if (SC_scursorManager.instance != null)
            SC_scursorManager.instance.gameObject.SetActive(false);

        if (instance == this)
            instance = null;
    }

    // =========================================================
    // SAVE MANAGER
    // =========================================================

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

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (number_of_stickers != null &&
            SC_StickerSaveSystem.instance != null)
        {
            number_of_stickers.text =
                SC_StickerSaveSystem.instance.stickerCount.ToString("D2");
        }

        if (edit != null && edit.action != null &&
            edit.action.WasPressedThisFrame())
        {
            if (!editing)
                StartEditMode();
            else
                QuitEditMode();
        }

        if (quit != null && quit.action != null &&
            quit.action.WasPerformedThisFrame())
        {
            if (SC_controller_manager.instance != null)
                SC_controller_manager.instance.keyboardAsController = false;

            if (SC_scursorManager.instance == null ||
                !SC_scursorManager.instance.grabing)
            {
                if (SC_screenshot_transition.instance != null)
                    SC_screenshot_transition.instance.Capture("HUB");
            }
        }

        if (quit2 != null && quit2.action != null &&
            quit2.action.WasPerformedThisFrame() && !editing)
        {
            if (SC_scursorManager.instance == null ||
                !SC_scursorManager.instance.grabing)
            {
                if (SC_screenshot_transition.instance != null)
                    SC_screenshot_transition.instance.Capture("HUB");
            }
        }

        if (!editing)
        {
            UpdateCursorMode();
        }
        else if (SC_scursorManager.instance != null)
        {
            SC_scursorManager.instance.enable_cursor();
        }
    }

    // =========================================================
    // CURSOR MODE
    // =========================================================

    private void UpdateCursorMode()
    {
        if (SC_controller_manager.instance == null)
            return;

        bool usingController =
            SC_controller_manager.instance.using_controller;

        if (usingController)
        {
            if (SC_scursorManager.instance != null)
                SC_scursorManager.instance.disable_cursor();

            if (EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == null)
            {
                SelectDefaultButton();
            }
        }
        else
        {
            if (SC_scursorManager.instance != null)
                SC_scursorManager.instance.enable_cursor();
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

    // =========================================================
    // EDIT MODE
    // =========================================================

    public void StartEditMode()
    {
        editing = true;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (SC_scursorManager.instance != null)
            SC_scursorManager.instance.enable_cursor();

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

        editing = false;

        if (anim != null)
            anim.SetTrigger("Off");

        if (SC_scursorManager.instance != null)
            SC_scursorManager.instance.disable_cursor();

        if (SC_controller_manager.instance != null &&
            SC_controller_manager.instance.using_controller)
        {
            SelectDefaultButton();
        }
    }

    public void quit_edit_mode()
    {
        QuitEditMode();
    }

    // =========================================================
    // STICKER INFO
    // =========================================================

    public void update_infos(SO_Sticker sticker, Button but)
    {
        if (sticker == null)
            return;

        button = but;

        bool isUnlocked = IsStickerUnlocked(sticker);

        // Image et informations communes.
        if (sprite_image != null)
            sprite_image.sprite = sticker.sticker_sprite;

        if (artist != null)
            artist.text = sticker.artist;

        UpdateStars(sticker.rarity);

        if (isUnlocked)
        {
            infos.SetActive(true);


            // ---------------------------------------------
            // STICKER DEBLOQUE
            // ---------------------------------------------

            if (sticker_name != null)
                sticker_name.text =
                    sticker.sticker_name.GetLocalizedString();

            if (sticker_description != null)
                sticker_description.text =
                    sticker.description.GetLocalizedString();

            if (sprite_image != null)
            {
                sprite_image.material =
                    sticker.special_mat != null
                        ? sticker.special_mat
                        : unlocked;
            }
        }
        else
        {
            infos.SetActive(false);


            // ---------------------------------------------
            // STICKER VERROUILLE
            // ---------------------------------------------

            if (sticker_name != null)
                sticker_name.text = "???";

            if (sticker_description != null)
            {
                sticker_description.text =
                    sticker.unlock_conditions.GetLocalizedString();
            }

            if (sprite_image != null)
                sprite_image.material = not_unlocked;
        }
    }

    // =========================================================
    // STARS
    // =========================================================

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
