using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SC_pause : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference pauseAction;
    public InputActionReference backAction;

    [Header("Menu Pause")]
    public GameObject pauseMenu;
    public GameObject tuto_window;
    public GameObject settings_;
    public GameObject buttons;
    public GameObject areyousure;
    public GameObject areyousureretry;
    public GameObject areyousurequit;

    [Header("Objet à masquer")]
    public GameObject objectToDisable;
    public GameObject objectToDisable2;
    public GameObject objectToEnable;
    public List<string> scenesToDisableObject = new List<string>();

    [Header("Réglages")]
    [SerializeField] private float inputCooldown = 0.2f;

    private bool isPaused = false;
    private bool tuto = false;
    private bool settings = false;
    private bool areyousure_ = false;
    private bool areyousure_retry = false;
    private bool areyousure_quit = false;

    // Empêche plusieurs inputs d'être traités trop rapidement
    private bool canUsePauseInput = true;
    private float inputTimer = 0f;

    private SC_screenshot_transition transition;


    private void OnEnable()
    {
        if (backAction != null)
        {
            backAction.action.Enable();
        }

        if (pauseAction != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += TogglePause;
        }
    }


    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= TogglePause;
            pauseAction.action.Disable();
        }

        if (backAction != null)
        {
            backAction.action.Disable();
        }
    }


    private void Start()
    {
        transition = SC_screenshot_transition.instance;

        isPaused = false;
        tuto = false;
        settings = false;
        areyousure_ = false;
        areyousure_retry = false;
        areyousure_quit = false;

        Time.timeScale = 1f;

        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        if (tuto_window != null)
            tuto_window.SetActive(false);

        if (areyousure != null)
            areyousure.SetActive(false);

        if (areyousureretry != null)
            areyousureretry.SetActive(false);

        if (areyousurequit != null)
            areyousurequit.SetActive(false);

        if (buttons != null)
            buttons.SetActive(true);
    }


    private void Update()
    {
        // Gestion du cooldown de l'input
        if (!canUsePauseInput)
        {
            inputTimer -= Time.unscaledDeltaTime;

            if (inputTimer <= 0f)
            {
                canUsePauseInput = true;
            }
        }

        if (!isPaused)
            return;

        // Empêche le bouton "Retour" de fermer le menu
        // immédiatement après son ouverture.
        if (!canUsePauseInput)
            return;

        if (backAction != null && backAction.action.WasPerformedThisFrame())
        {
            if (tuto)
            {
                Hide_tutorial();
            }
            else if (settings)
            {
                Hide_settings();
            }
            else if (areyousure_)
            {
                Hide_sure();
            }
            else if (areyousure_retry)
            {
                Hide_sure_retry();
            }
            else if (areyousure_quit)
            {
                Hide_sure_quit();
            }
            else
            {
                Close();
            }
        }
    }


    private void StartInputCooldown()
    {
        canUsePauseInput = false;
        inputTimer = inputCooldown;
    }


    private void UpdateObjectState()
    {
        if (objectToDisable == null)
            return;

        string currentScene = SceneManager.GetActiveScene().name;

        bool shouldDisable =
            scenesToDisableObject.Contains(currentScene);

        objectToDisable.SetActive(!shouldDisable);
        objectToDisable2.SetActive(!shouldDisable);

        if (objectToEnable != null)
        {
            objectToEnable.SetActive(shouldDisable);
        }
    }


    private void TogglePause(InputAction.CallbackContext context)
    {
        // Évite les doubles appels
        if (!canUsePauseInput)
            return;
        if (!SC_player.instance.gameObject.activeInHierarchy)
            return;

        // Si le joueur ne peut pas bouger, on ne permet pas
        // d'ouvrir le menu.
        if (!isPaused && SC_player.instance != null)
        {
            if (!SC_player.instance.canMove)
                return;

            if (!SC_player.instance.enabled)
                return;
        }

        // Pas de pause dans cette scène
        if (SceneManager.GetActiveScene().name == "Stickers")
            return;

        StartInputCooldown();

        UpdateObjectState();

        if (!isPaused)
        {
            OpenPause();
        }
        else
        {
            Close();
        }
    }


    private void OpenPause()
    {
        isPaused = true;
        buttons.GetComponent<SC_menu_navigation>().ResetFirstSelected();
        tuto = false;
        settings = false;

        // État du menu principal
        if (tuto_window != null)
            tuto_window.SetActive(false);
        if (settings_ != null)
            settings_.SetActive(false);

        if (buttons != null)
            buttons.SetActive(true);

        if (pauseMenu != null)
            pauseMenu.SetActive(true);

        // Musique
        if (SC_music_manager.instance != null)
        {
            SC_music_manager.instance.pause_music();
        }

        // Curseur
        if (SC_scursorManager.instance != null)
        {
            SC_scursorManager.instance.enable_cursor();
        }

        // Pause Unity
        Time.timeScale = 0f;

        // Désactive le joueur
        if (SC_player.instance != null)
        {
            SC_player.instance.enabled = false;

            if (SC_player.instance.anim != null)
            {
                SC_player.instance.anim.updateMode =
                    AnimatorUpdateMode.Normal;
            }
        }
    }


    public void Close()
    {
        isPaused = false;
        tuto = false;
        settings = false;

        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        if (tuto_window != null)
            tuto_window.SetActive(false);

        if (settings_ != null)
            settings_.SetActive(false);

        if (buttons != null)
            buttons.SetActive(true);

        // Musique
        if (SC_music_manager.instance != null)
        {
            SC_music_manager.instance.resume_music();
        }

        // Curseur
        if (SC_scursorManager.instance != null)
        {
            SC_scursorManager.instance.disable_cursor();
        }

        // Reprise du jeu
        Time.timeScale = 1f;

        if (SC_player.instance != null)
        {
            SC_player.instance.enabled = true;

            if (SC_player.instance.anim != null)
            {
                SC_player.instance.anim.updateMode =
                    AnimatorUpdateMode.UnscaledTime;
            }
        }

        // Évite qu'un deuxième input soit pris immédiatement
        StartInputCooldown();
    }


    public void Retry()
    {
        if (SC_music_manager.instance != null)
        {
            SC_music_manager.instance.resume_music();
        }

        if (SC_scursorManager.instance != null)
        {
            SC_scursorManager.instance.disable_cursor();
        }

        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        if (areyousureretry != null)
            areyousureretry.SetActive(false);

        isPaused = false;
        tuto = false;
        settings = false;

        Time.timeScale = 1f;

        if (SC_player.instance != null)
        {
            SC_player.instance.canMove = true;
            SC_player.instance.enabled = false;

            if (SC_player.instance.anim  != null)
            {
                SC_player.instance.anim.updateMode =
                    AnimatorUpdateMode.UnscaledTime;
            }
        }

        Scene currentScene = SceneManager.GetActiveScene();

        SceneManager.LoadScene(currentScene.buildIndex);
    }




    public void Show_settings()
    {
        settings = true;
        tuto = false;
        areyousure_retry = false;
        areyousure_ = false;
        areyousure_quit = false;

        if (settings_ != null)
            settings_.SetActive(true);

        if (buttons != null)
            buttons.SetActive(false);
        Time.timeScale = 0f;
    }


    public void Quit_game()
    {
        Application.Quit();
    }

    public void Show_tutorial()
    {
        tuto = true;
        settings = false;
        areyousure_ = false;
        areyousure_retry = false;
        areyousure_quit = false;

        if (tuto_window != null)
            tuto_window.SetActive(true);

        if (buttons != null)
            buttons.SetActive(false);
        Time.timeScale = 0f;
    }
    public void Hide_tutorial()
    {
        tuto = false;

        if (tuto_window != null)
            tuto_window.SetActive(false);

        if (buttons != null)
            buttons.SetActive(true);
        Time.timeScale = 0f;
    }


    public void Show_sure()
    {
        areyousure_ = true;
        settings = false;
        tuto = false;
        areyousure_quit = false;
        areyousure_retry = false;

        if (areyousure != null)
            areyousure.SetActive(true);

        if (buttons != null)
            buttons.SetActive(false);
        Time.timeScale = 0f;
    }
    public void Hide_sure()
    {
        areyousure_ = false;

        if (areyousure != null)
            areyousure.SetActive(false);

        if (buttons != null)
            buttons.SetActive(true);
        Time.timeScale = 0f;
    }


    public void Show_sure_retry()
    {
        areyousure_retry = true;
        settings = false;
        areyousure_ = false;
        tuto = false;
        areyousure_quit = false;

        if (areyousureretry != null)
            areyousureretry.SetActive(true);

        if (buttons != null)
            buttons.SetActive(false);
        Time.timeScale = 0f;
    }
    public void Hide_sure_retry()
    {
        areyousure_retry = false;

        if (areyousureretry != null)
            areyousureretry.SetActive(false);

        if (buttons != null)
            buttons.SetActive(true);
        Time.timeScale = 0f;
    }



    public void Show_sure_quit()
    {
        areyousure_quit = true;
        settings = false;
        areyousure_ = false;
        tuto = false;
        areyousure_retry = false;

        if (areyousurequit != null)
            areyousurequit.SetActive(true);

        if (buttons != null)
            buttons.SetActive(false);
        Time.timeScale = 0f;
    }
    public void Hide_sure_quit()
    {
        areyousure_quit = false;

        if (areyousurequit != null)
            areyousurequit.SetActive(false);

        if (buttons != null)
            buttons.SetActive(true);
        Time.timeScale = 0f;
    }


    public void Hide_settings()
    {
        settings = false;

        if (settings_ != null)
            settings_.SetActive(false);

        if (buttons != null)
            buttons.SetActive(true);
        Time.timeScale = 0f;
    }


    public void Give_up()
    {
        if (areyousure != null)
            areyousure.SetActive(false);

        if (SC_music_manager.instance != null)
        {
            SC_music_manager.instance.resume_music();
        }

        if (SC_scursorManager.instance != null)
        {
            SC_scursorManager.instance.disable_cursor();
        }

        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        isPaused = false;
        tuto = false;
        settings = false;

        Time.timeScale = 1f;

        if (SC_player.instance != null)
        {
            SC_player.instance.enabled = true;

            if (SC_player.instance.anim != null)
            {
                SC_player.instance.anim.updateMode =
                    AnimatorUpdateMode.UnscaledTime;
            }
        }

        if (transition != null)
        {
            if (PlayerPrefs.GetInt("Score") == 0)
            {
                transition.Capture("HUB");
            }
            else
            {
                transition.Capture("MoneyScene");
            }
        }
    }
}