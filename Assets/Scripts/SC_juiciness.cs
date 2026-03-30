using System.Collections;
using UnityEngine;

public class SC_juiciness : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Material flashMaterial;
    private Material defaultMaterial;

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

    private Vector3 originalScale;
    private Vector3 originalPosition;

    private Coroutine scaleRoutine;
    private Coroutine shakeRoutine;
    private Coroutine flashRoutine;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        defaultMaterial = spriteRenderer.material;
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
    }

    public void PlayJuice()
    {
        // Scale
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(SquashAndStretch());

        // Flash
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(Flash());

        // Shake
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(Shake());
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

        float t = 0f;

        // Étirement rapide au début, linéaire
        while (t < scaleDuration)
        {
            t += Time.deltaTime;
            float lerp = t / scaleDuration;
            // On utilise un easing plus agressif pour démarrer vite
            lerp = lerp * lerp; // ease-in rapide

            float newX = Mathf.Lerp(originalScale.x, stretchScale.x, lerp);
            float newY = Mathf.Lerp(originalScale.y, stretchScale.y, lerp);
            transform.localScale = new Vector3(newX, newY, originalScale.z);

            yield return null;
        }

        // Retour avec overshoot et rebond
        t = 0f;
        Vector3 overshootScale = originalScale + (stretchScale - originalScale) * 1.05f;

        while (t < scaleDuration)
        {
            t += Time.deltaTime;
            float lerp = t / scaleDuration;
            // easing élastique simple pour rebond
            lerp = Mathf.Sin(lerp * Mathf.PI * (0.2f + 2.5f * lerp * lerp * lerp)) * Mathf.Pow(1f - lerp, 2.2f) + 1f;

            float newX = Mathf.Lerp(overshootScale.x, originalScale.x, lerp);
            float newY = Mathf.Lerp(overshootScale.y, originalScale.y, lerp);
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
}