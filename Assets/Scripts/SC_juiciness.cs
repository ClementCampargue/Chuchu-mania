using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SC_juiciness : MonoBehaviour
{
    public Transform trs;

    [Header("Renderer")]
    public SpriteRenderer spriteRenderer;
    public Material flashMaterial;
    private Material defaultMaterial;

    [Header("Controller Rumble")]
    public bool rumble = true;
    public float rumbleLow = 0f;
    public float rumbleHigh = 0f;
    public float rumbleDuration = 0f;

    [Header("Screen Shake")]
    public float shakeAmplitude = 0f;
    public float shakeFrequency = 0f;
    public float shakeScreenDuration = 0f;

    [Header("Particles")]
    public List<ParticleSystem> ps = new();

    [Header("Audio")]
    public List<AudioSource> audio = new();

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

    [Header("Curves")]
    public AnimationCurve fovCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve timeScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);


    private Vector3 originalScale;
    private Vector3 originalPosition;

    private bool isScaling;
    private bool isShaking;
    private bool isFlashing;
    private bool isFreezing;


    void Awake()
    {
        ResetState();
    }


    void Start()
    {
        if (trs == null)
            trs = transform;

        originalScale = trs.localScale;


        if (flash)
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
                defaultMaterial = spriteRenderer.material;
        }
    }


    void OnEnable()
    {
        ResetState();
    }


    void ResetState()
    {
        isScaling = false;
        isShaking = false;
        isFlashing = false;
        isFreezing = false;

        Time.timeScale = 1f;
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
            PlayJuice();
    }


public void PlayJuice()
    {
        if (this == null || gameObject == null)
            return;

        if (!isActiveAndEnabled)
            return;

        // Particles
        if (ps != null)
        {
            foreach (ParticleSystem particle in ps)
            {
                if (particle != null)
                    particle.Play();
            }
        }

        // Audio
        if (audio != null)
        {
            foreach (AudioSource source in audio)
            {
                if (source != null)
                    source.Play();
            }
        }

        // Controller Rumble
        if (rumble && SC_rumbleManager.instance != null)
        {
            SC_rumbleManager.instance.PlayRumble(
                rumbleLow,
                rumbleHigh,
                rumbleDuration
            );
        }

        // Squash & Stretch
        if (!isScaling)
            StartCoroutine(SquashAndStretch());

        // Flash
        if (flash && !isFlashing)
            StartCoroutine(Flash());

        // Local Shake
        if (!isShaking)
            StartCoroutine(Shake());

        // Freeze Frame
        if (freeze && !isFreezing)
            StartCoroutine(FreezeFrame());
    }

    IEnumerator FreezeFrame()
    {
        isFreezing = true;

        float oldTimeScale = Time.timeScale;

        Time.timeScale = slowMoScale;


        yield return new WaitForSecondsRealtime(freezeDuration);


        Time.timeScale = oldTimeScale;

        isFreezing = false;
    }



    IEnumerator Flash()
    {
        isFlashing = true;


        if (spriteRenderer != null && flashMaterial != null)
            spriteRenderer.material = flashMaterial;


        yield return new WaitForSecondsRealtime(flashDuration);


        if (spriteRenderer != null)
            spriteRenderer.material = defaultMaterial;


        isFlashing = false;
    }




    IEnumerator SquashAndStretch()
    {
        isScaling = true;


        Vector3 baseScale = originalScale;
        Vector3 targetScale = baseScale;


        if (verticalStretch)
        {
            targetScale.y *= stretchAmount;
            targetScale.x *= squashAmount;
            targetScale.z *= squashAmount;
        }
        else
        {
            targetScale.x *= stretchAmount;
            targetScale.y *= squashAmount;
            targetScale.z *= squashAmount;
        }



        float t = 0;


        while (t < scaleDuration)
        {
            t += Time.unscaledDeltaTime;

            float value = Mathf.Clamp01(t / scaleDuration);

            trs.localScale =
                Vector3.Lerp(baseScale, targetScale, value * value);

            yield return null;
        }



        t = 0;


        while (t < scaleDuration)
        {
            t += Time.unscaledDeltaTime;

            float value = Mathf.Clamp01(t / scaleDuration);

            trs.localScale =
                Vector3.Lerp(
                    targetScale,
                    baseScale,
                    1f - Mathf.Pow(1f - value, 3f)
                );

            yield return null;
        }


        trs.localScale = baseScale;

        isScaling = false;
    }




    IEnumerator Shake()
    {
        isShaking = true;


        originalPosition = trs.localPosition;


        float t = 0;


        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;


            trs.localPosition =
                originalPosition +
                Random.insideUnitSphere * shakeIntensity;


            yield return null;
        }


        trs.localPosition = originalPosition;


        isShaking = false;
    }



    void OnDisable()
    {
        Time.timeScale = 1f;

        StopAllCoroutines();

        ResetState();
    }


    void OnDestroy()
    {
        StopAllCoroutines();

        Time.timeScale = 1f;
    }
}