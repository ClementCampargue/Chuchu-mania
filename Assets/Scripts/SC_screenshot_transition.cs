using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SC_screenshot_transition : MonoBehaviour
{
    public Camera targetCamera;
    public RenderTexture outputTexture;

    private RenderTexture tempRT;
    public static SC_screenshot_transition instance;
    public Animator anim;
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        RenderTexture previous = RenderTexture.active;

        RenderTexture.active = outputTexture;
        GL.Clear(true, true, Color.clear);

        RenderTexture.active = previous;
    }
    public void Capture(string scene)
    {
        // Sauvegarde état initial
        RenderTexture previousRT = targetCamera.targetTexture;

        // Assure la RT active
        RenderTexture.active = outputTexture;

        // Force le rendu dans la RenderTexture (ponctuel)
        targetCamera.targetTexture = outputTexture;
        targetCamera.Render();

        // Nettoyage état
        targetCamera.targetTexture = previousRT;
        RenderTexture.active = null;
        anim.enabled = true;
        SceneManager.LoadScene(scene);
    }

    public void restore_timescale()
    {
        Time.timeScale = 1;
    }
}