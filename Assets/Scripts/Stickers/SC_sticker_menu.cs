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
    private Material unlocked;
    public Material not_unlocked;

    [Header("Default Button")]
    public Button button;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        instance = this;
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
        if (SC_scursorManager.instance != null)
        {
            SC_scursorManager.instance.gameObject.SetActive(false);
            SC_scursorManager.instance.enable_cursor();
        }

        Invoke(nameof(DelayCursor), 0.25f);

        if (sprite_image != null)
        {
            unlocked = sprite_image.material;
        }

        if (SC_scursorManager.instance != null)
        {
            SC_scursorManager.instance.gameObject.SetActive(true);
        }
    }


    private void DelayCursor()
    {
        if (SC_scursorManager.instance != null)
        {
            SC_scursorManager.instance.gameObject.SetActive(true);
        }
    }


    private void OnEnable()
    {
        if (edit != null)
        {
            edit.action.Enable();
        }

        if (quit != null)
        {
            quit.action.Enable();
        }
    }


    private void OnDisable()
    {
        if (edit != null)
        {
            edit.action.Disable();
        }

        if (quit != null)
        {
            quit.action.Disable();
        }

        if (SC_scursorManager.instance != null)
        {
            SC_scursorManager.instance.gameObject.SetActive(false);
        }
    }


    private void OnDestroy()
    {
        if (SC_scursorManager.instance != null)
        {
            SC_scursorManager.instance.gameObject.SetActive(false);
        }

        if (instance == this)
        {
            instance = null;
        }
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


        // =====================================================
        // EDIT
        // =====================================================

        if (!editing &&
            edit != null &&
            edit.action.WasPressedThisFrame())
        {
            StartEditMode();
        }

        else if (editing &&
            edit != null &&
            edit.action.WasPressedThisFrame())
        {
            quit_edit_mode();
        }


        // =====================================================
        // QUIT
        // =====================================================

        if (quit != null &&
            quit.action.WasPerformedThisFrame())
        {
            if (SC_scursorManager.instance == null ||
                !SC_scursorManager.instance.grabing)
            {
                if (SC_screenshot_transition.instance != null)
                {
                    SC_screenshot_transition.instance.Capture("HUB");
                }
            }
        }
        if (quit2 != null &&
            quit2.action.WasPerformedThisFrame() && !editing)
        {
            if (SC_scursorManager.instance == null ||
                !SC_scursorManager.instance.grabing)
            {
                if (SC_screenshot_transition.instance != null)
                {
                    SC_screenshot_transition.instance.Capture("HUB");
                }
            }
        }


        // =====================================================
        // CURSEUR / MANETTE
        // =====================================================

        if (!editing)
        {
            UpdateCursorMode();
        }
        else
        {
            // En mode édition, la souris reste active.
            if (SC_scursorManager.instance != null)
            {
                SC_scursorManager.instance.enable_cursor();
            }
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
            // Manette :
            // on cache le curseur.
            if (SC_scursorManager.instance != null)
            {
                SC_scursorManager.instance.disable_cursor();
            }

            /*
             * IMPORTANT :
             *
             * On ne force plus button.Select()
             * à chaque frame.
             *
             * On le fait uniquement si absolument
             * aucun objet n'est sélectionné.
             */
            if (EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == null)
            {
                SelectDefaultButton();
            }
        }
        else
        {
            // Souris :
            // on affiche le curseur.
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


    // =========================================================
    // EDIT MODE
    // =========================================================

    public void StartEditMode()
    {
        editing = true;

        /*
         * On retire la sélection EventSystem.
         * Le mode édition est contrôlé par la souris.
         */
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (SC_scursorManager.instance != null)
        {
            SC_scursorManager.instance.enable_cursor();
        }

        if (anim != null)
        {
            anim.SetTrigger("On");
        }
    }


    public void start_edit_mode()
    {
        StartEditMode();
    }


    public void QuitEditMode()
    {
        // -----------------------------------------------------
        // SI UN STICKER EST ENCORE TENU
        // -----------------------------------------------------

        if (currentDraggingSticker != null)
        {
            SC_sticker_UI sticker = currentDraggingSticker;

            currentDraggingSticker = null;

            sticker.DeleteIfDragging();
        }


        editing = false;


        if (anim != null)
        {
            anim.SetTrigger("Off");
        }


        if (SC_scursorManager.instance != null)
        {
            SC_scursorManager.instance.disable_cursor();
        }


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


        // -----------------------------------------------------
        // STICKER DEBLOQUE
        // -----------------------------------------------------

        if (sticker.unlocked)
        {
            button = but;

            if (sticker_name != null)
            {
                sticker_name.text =
                    sticker.sticker_name;
            }

            if (sticker_description != null)
            {
                sticker_description.text =
                    sticker.description;
            }

            if (sprite_image != null)
            {
                if (sticker.special_mat != null)
                {
                    sprite_image.material =
                        sticker.special_mat;
                }
                else
                {
                    sprite_image.material =
                        unlocked;
                }
            }
        }


        // -----------------------------------------------------
        // STICKER VERROUILLE
        // -----------------------------------------------------

        else
        {
            if (sticker_name != null)
            {
                sticker_name.text = "???";
            }

            if (sticker_description != null)
            {
                sticker_description.text =
                    sticker.unlock_conditions;
            }

            if (sprite_image != null)
            {
                sprite_image.material =
                    not_unlocked;
            }
        }


        // -----------------------------------------------------
        // INFOS COMMUNES
        // -----------------------------------------------------

        if (artist != null)
        {
            artist.text = sticker.artist;
        }

        if (sprite_image != null)
        {
            sprite_image.sprite =
                sticker.sticker_sprite;
        }

        UpdateStars(sticker.rarity);
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
            {
                star.SetActive(false);
            }
        }

        rarity = Mathf.Clamp(
            rarity,
            0,
            stars.Count
        );

        for (int i = 0; i < rarity; i++)
        {
            if (stars[i] != null)
            {
                stars[i].SetActive(true);
            }
        }
    }
}