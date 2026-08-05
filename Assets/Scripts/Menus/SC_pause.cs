using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SC_pause : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference pauseAction;

    [Header("Menu Pause")]
    public GameObject pauseMenu;
    public GameObject tuto_window;

    [Header("Objet à masquer")]
    public GameObject objectToDisable;
    public List<string> scenesToDisableObject = new List<string>();

    private bool isPaused = false;
    private bool tuto = false;
    private SC_screenshot_transition transition;

    private void OnEnable()
    {
        pauseAction.action.Enable();
        pauseAction.action.performed += TogglePause;
    }

    private void OnDisable()
    {
        pauseAction.action.performed -= TogglePause;
        pauseAction.action.Disable();
    }

    private void Start()
    {
        transition = SC_screenshot_transition.instance;
        pauseMenu.SetActive(false);

    }

    private void UpdateObjectState()
    {
        if (objectToDisable == null)
            return;

        string currentScene = SceneManager.GetActiveScene().name;

        // Désactive l'objet si la scène est dans la liste
        objectToDisable.SetActive(!scenesToDisableObject.Contains(currentScene));
    }

    private void TogglePause(InputAction.CallbackContext context)
    {
        if (tuto)
        {
            Hide_tutorial();
            return;
        }
        UpdateObjectState();
        isPaused = !isPaused;

        pauseMenu.SetActive(isPaused);
        Hide_tutorial();

        Time.timeScale = isPaused ? 0f : 1f;
        SC_player.instance.canMove = !isPaused;
        SC_player.instance.anim_.updateMode = isPaused
            ? AnimatorUpdateMode.Normal
            : AnimatorUpdateMode.UnscaledTime;
    }

    public void close()
    {
        pauseMenu.SetActive(false);
        Hide_tutorial();
        isPaused = false;
        SC_player.instance.canMove = true;
        SC_player.instance.anim_.updateMode = AnimatorUpdateMode.UnscaledTime;
        Time.timeScale = 1;
    }

    public void Retry()
    {
        pauseMenu.SetActive(false);

        isPaused = false;
        SC_player.instance.canMove = true;
        SC_player.instance.anim_.updateMode = AnimatorUpdateMode.UnscaledTime;
        Time.timeScale = 1;
        SC_player.instance.enabled = false;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void Show_tutorial()
    {
        tuto = true;
        tuto_window.SetActive(true);
    }

    public void Show_settings()
    {
        tuto = false;
        tuto_window.SetActive(true);
    }

    public void Hide_tutorial()
    {
        tuto_window.SetActive(false);
    }

    public void Give_up()
    {
        pauseMenu.SetActive(false);

        isPaused = false;

        SC_player.instance.canMove = true;
        SC_player.instance.anim_.updateMode = AnimatorUpdateMode.UnscaledTime;
        Time.timeScale = 1;
        transition.Capture("HUB");
    }
}