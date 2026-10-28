
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SC_pause : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference moveaction;
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

    public bool canclose = true;
    public bool isPaused = false;

    private bool tuto = false;
    private bool settings = false;
    private bool areyousure_ = false;
    private bool areyousure_retry = false;
    private bool areyousure_quit = false;

    private bool canUsePauseInput = true;
    private float inputTimer = 0f;

    private SC_screenshot_transition transition;

    public AudioMixer group;
    public AudioSource cancel;

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed += TogglePause;
            pauseAction.action.Enable();
        }

        if (backAction != null)
        {
            backAction.action.Enable();
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
        canclose = true;

        tuto = false;
        settings = false;
        areyousure_ = false;
        areyousure_retry = false;
        areyousure_quit = false;

        canUsePauseInput = true;
        inputTimer = 0f;

        Time.timeScale = 1f;

        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        if (tuto_window != null)
            tuto_window.SetActive(false);

        if (settings_ != null)
            settings_.SetActive(false);

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
        if (!canUsePauseInput)
        {
            inputTimer -= Time.unscaledDeltaTime;

            if (inputTimer <= 0f)
                canUsePauseInput = true;
        }

        if (!isPaused || !canUsePauseInput)
            return;
        moveaction.action.Enable();
        if (backAction != null &&
            backAction.action.WasPressedThisFrame())
        {
            HandleCancel();
        }
    }

    private void StartInputCooldown()
    {
        canUsePauseInput = false;
        inputTimer = inputCooldown;
    }

    private void UpdateObjectState()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        bool shouldDisable =
            scenesToDisableObject.Contains(currentScene);

        if (objectToDisable != null)
            objectToDisable.SetActive(!shouldDisable);

        if (objectToDisable2 != null)
            objectToDisable2.SetActive(!shouldDisable);

        if (objectToEnable != null)
            objectToEnable.SetActive(shouldDisable);
    }

    private void TogglePause(InputAction.CallbackContext context)
    {
        if (!canUsePauseInput)
            return;

        if (SC_player.instance == null)
            return;

        if (!SC_player.instance.gameObject.activeInHierarchy)
            return;

        if (SceneManager.GetActiveScene().name == "Stickers")
            return;

        if (!isPaused)
        {
            if (!SC_player.instance.canMove)
                return;

            if (!SC_player.instance.enabled)
                return;

            StartInputCooldown();
            UpdateObjectState();
            OpenPause();
        }
        else
        {
            if (HasOpenSubmenu())
                return;

            if (!canclose)
                return;

            StartInputCooldown();
            Close();
        }
    }

    private bool HasOpenSubmenu()
    {
        return tuto ||
               settings ||
               areyousure_ ||
               areyousure_retry ||
               areyousure_quit;
    }

    private void HandleCancel()
    {
        if (!isPaused)
            return;

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
            if (!canUsePauseInput)
                return;

            if (SC_player.instance == null)
                return;

            if (!SC_player.instance.gameObject.activeInHierarchy)
                return;

            if (SceneManager.GetActiveScene().name == "Stickers")
                return;

            if (!isPaused)
            {
                if (!SC_player.instance.canMove)
                    return;

                if (!SC_player.instance.enabled)
                    return;

                StartInputCooldown();
                UpdateObjectState();
                OpenPause();
            }
            else
            {
                if (HasOpenSubmenu())
                    return;

                if (!canclose)
                    return;

                StartInputCooldown();
                Close();
            }
        }

    }

    private void OpenPause()
    {
        isPaused = true;
        canclose = true;

        if (group != null)
            group.SetFloat("LevelVolume", -1000f);

        CloseAllSubmenus();

        if (buttons != null)
        {
            buttons.SetActive(true);

            SC_menu_navigation navigation =
                buttons.GetComponent<SC_menu_navigation>();

            if (navigation != null)
                navigation.ResetFirstSelected();
        }

        if (pauseMenu != null)
            pauseMenu.SetActive(true);

        if (SC_music_manager.instance != null)
            SC_music_manager.instance.pause_music();

        if (SC_scursorManager.instance != null)
            SC_scursorManager.instance.enable_cursor();

        if (SC_time_manager.instance != null)
            SC_time_manager.instance.Pause();

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
        if (!isPaused || HasOpenSubmenu() || !canclose)
            return;

        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        CloseAllSubmenus();

        isPaused = false;
        canclose = true;

        if (buttons != null)
            buttons.SetActive(true);

        if (group != null)
            group.SetFloat("LevelVolume", 0f);

        if (SC_music_manager.instance != null)
            SC_music_manager.instance.resume_music();

        if (SC_scursorManager.instance != null)
            SC_scursorManager.instance.disable_cursor();

        if (SC_time_manager.instance != null)
            SC_time_manager.instance.Resume();

        if (SC_player.instance != null)
        {
            SC_player.instance.enabled = true;

            if (SC_player.instance.anim != null)
            {
                SC_player.instance.anim.updateMode =
                    AnimatorUpdateMode.UnscaledTime;
            }
        }

        StartInputCooldown();
    }

    public void Retry()
    {
        if (SC_music_manager.instance != null)
            SC_music_manager.instance.resume_music();

        if (SC_scursorManager.instance != null)
            SC_scursorManager.instance.disable_cursor();

        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        if (areyousureretry != null)
            areyousureretry.SetActive(false);

        isPaused = false;
        canclose = true;

        CloseAllSubmenus();

        if (SC_time_manager.instance != null)
            SC_time_manager.instance.Resume();

        if (SC_player.instance != null)
        {
            SC_player.instance.canMove = true;
            SC_player.instance.enabled = false;

            if (SC_player.instance.anim != null)
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
        CloseAllSubmenus();

        settings = true;
        canclose = false;

        if (settings_ != null)
            settings_.SetActive(true);

        if (buttons != null)
            buttons.SetActive(false);
    }

    public void Hide_settings()
    {
        if (!settings)
            return;

        PlayCancel();

        settings = false;

        if (settings_ != null)
            settings_.SetActive(false);

        if (buttons != null)
            buttons.SetActive(true);

        canclose = true;
    }

    public void Show_tutorial()
    {
        CloseAllSubmenus();

        tuto = true;
        canclose = false;

        if (tuto_window != null)
            tuto_window.SetActive(true);

        if (buttons != null)
            buttons.SetActive(false);
    }

    public void Hide_tutorial()
    {
        if (!tuto)
            return;

        PlayCancel();

        tuto = false;

        if (tuto_window != null)
            tuto_window.SetActive(false);

        if (buttons != null)
            buttons.SetActive(true);

        canclose = true;
    }

    public void Show_sure()
    {
        CloseAllSubmenus();

        areyousure_ = true;
        canclose = false;

        if (areyousure != null)
            areyousure.SetActive(true);

        if (buttons != null)
            buttons.SetActive(false);
    }

    public void Hide_sure()
    {
        if (!areyousure_)
            return;

        PlayCancel();

        areyousure_ = false;

        if (areyousure != null)
            areyousure.SetActive(false);

        if (buttons != null)
            buttons.SetActive(true);

        canclose = true;
    }

    public void Show_sure_retry()
    {
        CloseAllSubmenus();

        areyousure_retry = true;
        canclose = false;

        if (areyousureretry != null)
            areyousureretry.SetActive(true);

        if (buttons != null)
            buttons.SetActive(false);
    }

    public void Hide_sure_retry()
    {
        if (!areyousure_retry)
            return;

        PlayCancel();

        areyousure_retry = false;

        if (areyousureretry != null)
            areyousureretry.SetActive(false);

        if (buttons != null)
            buttons.SetActive(true);

        canclose = true;
    }

    public void Show_sure_quit()
    {
        CloseAllSubmenus();

        areyousure_quit = true;
        canclose = false;

        if (areyousurequit != null)
            areyousurequit.SetActive(true);

        if (buttons != null)
            buttons.SetActive(false);
    }

    public void Hide_sure_quit()
    {
        if (!areyousure_quit)
            return;

        PlayCancel();

        areyousure_quit = false;

        if (areyousurequit != null)
            areyousurequit.SetActive(false);

        if (buttons != null)
            buttons.SetActive(true);

        canclose = true;
    }

    private void CloseAllSubmenus()
    {
        tuto = false;
        settings = false;
        areyousure_ = false;
        areyousure_retry = false;
        areyousure_quit = false;

        if (tuto_window != null)
            tuto_window.SetActive(false);

        if (settings_ != null)
            settings_.SetActive(false);

        if (areyousure != null)
            areyousure.SetActive(false);

        if (areyousureretry != null)
            areyousureretry.SetActive(false);

        if (areyousurequit != null)
            areyousurequit.SetActive(false);
    }

    private void PlayCancel()
    {
        if (cancel != null)
            cancel.Play();
    }

    public void Quit_game()
    {
        Application.Quit();
    }

    public void Give_up()
    {
        if (areyousure != null)
            areyousure.SetActive(false);

        if (SC_music_manager.instance != null)
            SC_music_manager.instance.resume_music();

        if (SC_scursorManager.instance != null)
            SC_scursorManager.instance.disable_cursor();

        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        isPaused = false;
        canclose = true;

        CloseAllSubmenus();

        if (SC_time_manager.instance != null)
            SC_time_manager.instance.Resume();

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
                transition.Capture("HUB");
            else
                transition.Capture("MoneyScene");
        }
    }
}
