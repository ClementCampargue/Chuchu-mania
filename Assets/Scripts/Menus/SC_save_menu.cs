using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SC_save_menu : MonoBehaviour
{
    [Header("Scenes")]
    public string scene;
    public string scene_first_launch;
    public SC_Button button;
    [Header("Save Manager")]
    private SC_SaveManager manager;

    public InputActionReference delete;
    public InputActionReference quit;

    public GameObject delete_menu;
    public GameObject quit_menu;
    public GameObject menu;
    public bool menu_opened;
    public GameObject delete_prompt;

    public List<GameObject> files_on;
    public bool canact;
    private void Awake()
    {
        Debug.Log(Application.persistentDataPath);

        if (manager == null)
        {
            manager = SC_SaveManager.instance;
        }
    }

    private void Start()
    {
        UpdateCursorMode();
        update_files();
        Invoke("delay", 1);
    }
    void delay()
    {
        canact = true;
    }
    void update_files()
    {
        for(int i = 0; i < files_on.Count; i++)
        {
            files_on[i].SetActive(manager.SlotExists(i));
        }
    }

    private void OnEnable()
    {
        if (delete != null)
            delete.action.Enable();

        if (quit != null)
            quit.action.Enable();
    }

    private void Update()
    {if (!canact) return;
        delete_prompt.SetActive(manager.SlotExists(manager.activeSlot));
        if (menu_opened)
        {
            if (quit.action.WasPerformedThisFrame())
            {
                close_menu();
            }
        }
        else
        {
            if (quit.action.WasPerformedThisFrame())
            {
                menu.SetActive(false);
                quit_menu.SetActive(true);
                menu_opened = true;
            }
            if (delete.action.WasPressedThisFrame() && manager.SlotExists(manager.activeSlot))
            {
                Debug.Log("cbon");
                menu.SetActive(false);
                menu_opened = true;
                delete_menu.SetActive(true); 
            }
        }
    }

    public void close_menu()
    {
        menu_opened = false;
        menu.SetActive(true);
        quit_menu.SetActive(false);
        delete_menu.SetActive(false);
    }

    public void quit_game()
    {
        Application.Quit();
    }
    public void delete_save()
    {
        close_menu();
        manager.DeleteSlot(manager.activeSlot);
        update_files();
    }

    // Ouvre la scène appropriée selon le premier lancement.
    public void load_scene()
    {
        if (SC_screenshot_transition.instance == null)
        {
            Debug.LogError("SC_screenshot_transition introuvable !");
            return;
        }

        if (SC_SaveManager.instance.data.firtlaunch)
        {
            SC_SaveManager.instance.data.firtlaunch = false;
            SC_SaveManager.instance.SaveGame();
            SC_screenshot_transition.instance.Capture(scene_first_launch);
        }
        else
        {
            SC_screenshot_transition.instance.Capture(scene);
        }
    }

    // Sélectionne le slot de sauvegarde.
    public void update_current_slot(int i)
    {
        if (!CheckManager())
            return;

        if (!manager.IsValidSlot(i))
        {
            Debug.LogWarning("Slot invalide : " + i);
            return;
        }

        manager.activeSlot = i;

        Debug.Log("Slot sélectionné : " + i);
    }

    // Clique sur un slot : charge la sauvegarde
    // si elle existe, sinon crée une nouvelle partie.
    public void click_slot()
    {
        if (!CheckManager())
            return;

        int slot = manager.activeSlot;

        if (!manager.IsValidSlot(slot))
        {
            Debug.LogWarning("Aucun slot valide sélectionné.");
            return;
        }

        if (manager.SlotExists(slot))
        {
            load_slot();
        }
        else
        {
            create_slot();
        }
    }

    // Crée une nouvelle sauvegarde dans le slot sélectionné.
    public void create_slot()
    {
        if (!CheckManager())
            return;

        int slot = manager.activeSlot;

        if (!manager.IsValidSlot(slot))
        {
            Debug.LogWarning("Impossible de créer la sauvegarde : slot invalide.");
            return;
        }

        // Évite d'écraser une sauvegarde existante.
        if (manager.SlotExists(slot))
        {
            Debug.LogWarning("Une sauvegarde existe déjà dans ce slot.");
            return;
        }

        if (!manager.CreateNewSlot(slot))
        {
            Debug.LogError("Échec de la création de la sauvegarde.");
            return;
        }
        update_files();

    }

    // Charge les données avant de lancer la transition.
    public void load_slot()
    {
        if (!CheckManager())
            return;

        int slot = manager.activeSlot;

        if (!manager.IsValidSlot(slot))
        {
            Debug.LogWarning("Slot invalide.");
            return;
        }

        if (!manager.SlotExists(slot))
        {
            Debug.LogWarning("Aucune sauvegarde dans le slot " + slot);
            return;
        }

        if (!manager.LoadSlot(slot))
        {
            Debug.LogError("Impossible de charger le slot " + slot);
            return;
        }

        Debug.Log("Partie chargée : slot " + slot);

        OpenHub();
    }

    // Supprime la sauvegarde sélectionnée.
    public void delete_slot()
    {
        if (!CheckManager())
            return;

        int slot = manager.activeSlot;

        if (!manager.IsValidSlot(slot))
        {
            Debug.LogWarning("Slot invalide.");
            return;
        }

        if (!manager.SlotExists(slot))
        {
            Debug.LogWarning("Le slot " + slot + " est déjà vide.");
            return;
        }

        if (manager.DeleteSlot(slot))
        {
            Debug.Log("Sauvegarde supprimée : slot " + slot);
        }
        else
        {
            Debug.LogError("Impossible de supprimer le slot " + slot);
        }
    }

    // Lance la transition vers le HUB.
    private void OpenHub()
    {
        if (SC_screenshot_transition.instance == null)
        {
            Debug.LogError("SC_screenshot_transition introuvable !");
            return;
        }

        if (SC_SaveManager.instance.data.firtlaunch)
        {
            SC_SaveManager.instance.data.firtlaunch = false;
            SC_SaveManager.instance.SaveGame();
            SC_screenshot_transition.instance.Capture(scene_first_launch);
        }
        else
        {
            SC_screenshot_transition.instance.Capture(scene);
        }
    }

    // Vérifie que le gestionnaire existe.
    private bool CheckManager()
    {
        if (manager == null)
        {
            manager = SC_SaveManager.instance;
        }

        if (manager == null)
        {
            Debug.LogError("SC_SaveManager introuvable !");
            return false;
        }

        return true;
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
