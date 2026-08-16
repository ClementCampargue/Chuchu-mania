using System.Collections;
using UnityEngine;

public class SC_time_manager : MonoBehaviour
{
    public static SC_time_manager instance;

    [Header("Default")]
    [SerializeField] private float normalTimeScale = 1f;

    [Header("Slow Motion")]
    [SerializeField] private float defaultSlowMoScale = 0.05f;

    private bool isPaused;
    private bool isFrozen;

    private float slowMoScale;
    private bool slowMotionRequested;

    private Coroutine freezeCoroutine;

    public bool IsPaused => isPaused;
    public bool IsFrozen => isFrozen;
    public bool IsSlowMotion => slowMotionRequested;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        Time.timeScale = normalTimeScale;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            Time.timeScale = normalTimeScale;
            instance = null;
        }
    }

    private void Update()
    {
        UpdateTimeScale();
    }

    // =========================================================
    // PAUSE
    // =========================================================

    public void Pause()
    {
        isPaused = true;
        UpdateTimeScale();
    }

    public void Resume()
    {
        isPaused = false;
        UpdateTimeScale();
    }

    public void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

    // =========================================================
    // FREEZE FRAME
    // =========================================================

    public void FreezeFrame(float duration)
    {
        FreezeFrame(duration, defaultSlowMoScale);
    }

    public void FreezeFrame(float duration, float timeScale)
    {
        if (duration <= 0f)
            return;

        slowMoScale = Mathf.Max(0f, timeScale);

        if (freezeCoroutine != null)
            StopCoroutine(freezeCoroutine);

        freezeCoroutine = StartCoroutine(FreezeCoroutine(duration));
    }

    private IEnumerator FreezeCoroutine(float duration)
    {
        isFrozen = true;

        UpdateTimeScale();

        yield return new WaitForSecondsRealtime(duration);

        isFrozen = false;
        freezeCoroutine = null;

        UpdateTimeScale();
    }

    // =========================================================
    // SLOW MOTION
    // =========================================================

    public void SetSlowMotion(float timeScale)
    {
        slowMotionRequested = true;
        slowMoScale = Mathf.Max(0f, timeScale);

        UpdateTimeScale();
    }

    public void SetSlowMotion()
    {
        SetSlowMotion(defaultSlowMoScale);
    }

    public void StopSlowMotion()
    {
        slowMotionRequested = false;

        UpdateTimeScale();
    }

    // =========================================================
    // TIME SCALE
    // =========================================================

    private void UpdateTimeScale()
    {
        // La pause est prioritaire
        if (isPaused)
        {
            Time.timeScale = 0f;
            return;
        }

        // Le freeze est prioritaire sur le slow motion normal
        if (isFrozen)
        {
            Time.timeScale = slowMoScale;
            return;
        }

        // Slow motion
        if (slowMotionRequested)
        {
            Time.timeScale = slowMoScale;
            return;
        }

        // Temps normal
        Time.timeScale = normalTimeScale;
    }

    // =========================================================
    // RESET
    // =========================================================

    public void ResetTime()
    {
        isPaused = false;
        isFrozen = false;
        slowMotionRequested = false;

        if (freezeCoroutine != null)
        {
            StopCoroutine(freezeCoroutine);
            freezeCoroutine = null;
        }

        Time.timeScale = normalTimeScale;
    }
}