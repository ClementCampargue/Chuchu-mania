using System.Collections;
using UnityEngine;

public class SC_juiciness : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Material flashMaterial;
    private Material defaultMaterial;

    [Header("Particles")]
    public ParticleSystem ps;

    [Header("Flash Settings")]
    public float flashDuration = 0.05f;

    [Header("Squash & Stretch Settings")]
    public float stretchAmount = 1.2f;
    public float squashAmount = 0.8f;
    public float scaleDuration = 0.1f;
    public bool verticalStretch = true;

    [Header("Shake Settings")]
    public float shakeDuration = 0.15f;
    public float shakeIntensity = 0.1f;

    [Header("Freeze Frame Settings")]
    public bool freeze = false;
    public float freezeDuration = 0.05f;

    [Header("Zoom Settings")]
    public float zoomAmount = 2f;
    public float zoomDuration = 0.1f;

    private Vector3 originalScale;
    private Vector3 originalPosition;
    private float originalCamSize;

    private Coroutine scaleRoutine;
    private Coroutine shakeRoutine;
    private Coroutine flashRoutine;
    private Coroutine freezeRoutine;
    private Coroutine zoomRoutine;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        defaultMaterial = spriteRenderer.material;
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;

        if (Camera.main != null)
            originalCamSize = Camera.main.orthographicSize;
    }

    public void PlayJuice()
    {
        if (ps != null)
        {
            ps.Play();
        }

        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(SquashAndStretch());

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(Flash());

        if (shakeRoutine != null && shakeIntensity != 0) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(Shake());
        if (freeze)
        {
            if (freezeRoutine != null && freezeDuration != 0) StopCoroutine(freezeRoutine);
            freezeRoutine = StartCoroutine(FreezeFrame());
        }

        if (zoomRoutine != null && zoomAmount != 0) StopCoroutine(zoomRoutine);
        zoomRoutine = StartCoroutine(Zoom());
    }

    IEnumerator Flash()
    {
        spriteRenderer.material = flashMaterial;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.material = defaultMaterial;
    }

    IEnumerator SquashAndStretch()
    {
        transform.localScale = originalScale;

        Vector3 stretchScale = originalScale;
        if (verticalStretch)
        {
            stretchScale.y = originalScale.y * stretchAmount;
            stretchScale.x = originalScale.x * squashAmount;
        }
        else
        {
            stretchScale.x = originalScale.x * stretchAmount;
            stretchScale.y = originalScale.y * squashAmount;
        }

        // Phase 1: Stretch
        float t = 0f;
        while (t < scaleDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / scaleDuration);
            lerp = lerp * lerp; // ease-in rapide

            float newX = Mathf.Lerp(originalScale.x, stretchScale.x, lerp);
            float newY = Mathf.Lerp(originalScale.y, stretchScale.y, lerp);
            transform.localScale = new Vector3(newX, newY, originalScale.z);

            yield return null;
        }

        // Phase 2: Overshoot vers la position originale
        t = 0f;
        Vector3 overshootScale = originalScale + (stretchScale - originalScale) * 1.05f;

        while (t < scaleDuration)
        {
            t += Time.deltaTime;
            float rawLerp = Mathf.Clamp01(t / scaleDuration);

            // Calcul d'overshoot sûr sans NaN
            float overshootLerp = Mathf.Sin(rawLerp * Mathf.PI * (0.2f + 2.5f * Mathf.Pow(rawLerp, 3f)))
                                  * Mathf.Pow(Mathf.Max(0f, 1f - rawLerp), 2.2f) + 1f;

            overshootLerp = Mathf.Clamp(overshootLerp, 0f, 1.5f); // sécurité

            float newX = Mathf.Lerp(overshootScale.x, originalScale.x, overshootLerp);
            float newY = Mathf.Lerp(overshootScale.y, originalScale.y, overshootLerp);
            transform.localScale = new Vector3(newX, newY, originalScale.z);

            yield return null;
        }

        transform.localScale = originalScale;
    }

    IEnumerator Shake()
    {
        originalPosition = transform.localPosition;
        float t = 0f;

        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            Vector3 offset = Random.insideUnitCircle * shakeIntensity;
            transform.localPosition = originalPosition + offset;
            yield return null;
        }

        transform.localPosition = originalPosition;
    }

    IEnumerator FreezeFrame()
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(freezeDuration);
        Time.timeScale = originalTimeScale;
    }

    IEnumerator Zoom()
    {
        if (Camera.main == null) yield break;

        float t = 0f;
        float targetSize = originalCamSize / zoomAmount;

        while (t < zoomDuration)
        {
            t += Time.unscaledDeltaTime;
            Camera.main.orthographicSize = Mathf.Lerp(originalCamSize, targetSize, Mathf.Clamp01(t / zoomDuration));
            yield return null;
        }

        t = 0f;
        while (t < zoomDuration)
        {
            t += Time.unscaledDeltaTime;
            Camera.main.orthographicSize = Mathf.Lerp(targetSize, originalCamSize, Mathf.Clamp01(t / zoomDuration));
            yield return null;
        }

        Camera.main.orthographicSize = originalCamSize;
    }
}