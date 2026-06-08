using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SC_juiciness : MonoBehaviour
{
    public Transform trs;

    [Header("Renderer")]
    public SpriteRenderer spriteRenderer;
    public Material flashMaterial;
    private Material defaultMaterial;

    [Header("Controller Rumble")]
    public bool rumble = true;
    public float rumbleLow = 0.1f;
    public float rumbleHigh = 0.3f;
    public float rumbleDuration = 0.1f;

    [Header("Screen Shake")]
    public float shakeAmplitude = 0f;
    public float shakeFrequency = 0f;
    public float shakeScreenDuration = 0f;

    [Header("Particles")]
    public List<ParticleSystem> ps;


    [Header("Audio")]
    public List<AudioSource> audio;

    [Header("Flash")]
    public bool flash = true;
    public float flashDuration = 0.05f;

    [Header("Squash & Stretch")]
    public float stretchAmount = 1.2f;
    public float squashAmount = 0.8f;
    public float scaleDuration = 0.1f;
    public bool verticalStretch = true;

    [Header("Local Shake")]
    public float shakeDuration = 0.15f;
    public float shakeIntensity = 0.1f;

    [Header("Freeze Frame")]
    public bool freeze = false;
    public float freezeDuration = 0.05f;
    public float slowMoScale = 0.05f;
    public float freezeRecoverDuration = 0.15f;

    [Header("Curves")]
    public AnimationCurve fovCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve timeScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 originalScale;
    private Vector3 originalPosition;

    private Coroutine scaleRoutine;
    private Coroutine shakeRoutine;
    private Coroutine flashRoutine;
    private Coroutine freezeRoutine;

    private bool isFreezing;

    void Start()
    {
        trs ??= transform;

        if (flash)
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            defaultMaterial = spriteRenderer.material;
        }
        if (trs != null)
            originalScale = trs.localScale;

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
            PlayJuice();
    }

    public void PlayJuice()
    {
        // Rumble
        if (rumble)
            SC_rumbleManager.instance.PlayRumble(rumbleLow, rumbleHigh, rumbleDuration);




        // Particles
        if (ps.Count > 0)
        {
            foreach(ParticleSystem ps in ps)
            {
                ps.Play();
            }
        }
        if (audio.Count > 0)
        {
            foreach(AudioSource audio in audio)
            {
                audio.Play();
            }
        }

        // Squash
        RestartCoroutine(ref scaleRoutine, SquashAndStretch());

        // Flash
        if (flash)
            RestartCoroutine(ref flashRoutine, Flash());

        // Local shake
        RestartCoroutine(ref shakeRoutine, Shake());

        // Freeze frame (independent)
        if (freeze && !isFreezing && Time.timeScale == 1f)
            freezeRoutine = StartCoroutine(FreezeFrame());
    }

    // ---------------- CAMERA ZOOM (SEPARATED) ---------------- 



    // ---------------- FREEZE FRAME ----------------

    IEnumerator FreezeFrame()
    {
        isFreezing = true;

        float originalTimeScale = Time.timeScale;
        Time.timeScale = slowMoScale;


        yield return new WaitForSecondsRealtime(freezeDuration);

        Time.timeScale = originalTimeScale;



        isFreezing = false;
    }

    // ---------------- EFFECTS ----------------

    IEnumerator Flash()
    {
        spriteRenderer.material = flashMaterial;
        yield return new WaitForSecondsRealtime(flashDuration);
        spriteRenderer.material = defaultMaterial;
    }

    IEnumerator SquashAndStretch()
    {
        Vector3 baseScale = originalScale;

        Vector3 stretchScale = baseScale;

        if (verticalStretch)
        {
            stretchScale.y *= stretchAmount;
            stretchScale.x *= squashAmount;
            stretchScale.z *= squashAmount;
        }
        else
        {
            stretchScale.x *= stretchAmount;
            stretchScale.y *= squashAmount;
            stretchScale.z *= squashAmount;
        }

        float t = 0f;

        while (t < scaleDuration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(t / scaleDuration);
            trs.localScale = Vector3.Lerp(baseScale, stretchScale, lerp * lerp);
            yield return null;
        }

        t = 0f;

        while (t < scaleDuration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(t / scaleDuration);
            trs.localScale = Vector3.Lerp(stretchScale, baseScale, 1f - Mathf.Pow(1f - lerp, 3f));
            yield return null;
        }

        trs.localScale = baseScale;
    }

    IEnumerator Shake()
    {
        originalPosition = trs.localPosition;

        float t = 0f;

        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;
            trs.localPosition = originalPosition + Random.insideUnitSphere * shakeIntensity;
            yield return null;
        }

        trs.localPosition = originalPosition;
    }

    // ---------------- UTILS ----------------

    void RestartCoroutine(ref Coroutine routine, IEnumerator enumerator)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(enumerator);
    }
}