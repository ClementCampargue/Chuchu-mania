using System.Collections;
using UnityEngine;

public class SpriteShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float strength = 0.1f;
    [SerializeField] private float duration = 0.2f;

    [Header("Loop")]
    [SerializeField] private bool loop = false;

    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        originalPosition = transform.localPosition;
    }

    // Shake pendant la durée définie dans l'Inspector
    public void Shake()
    {
        Shake(duration);
    }

    // Shake pendant une durée précise
    public void Shake(float customDuration)
    {
        StopShake();

        shakeCoroutine = StartCoroutine(ShakeCoroutine(customDuration));
    }

    // Lance un shake en boucle
    public void StartLoop()
    {
        StopShake();

        shakeCoroutine = StartCoroutine(ShakeLoopCoroutine());
    }

    // Arrête le loop
    public void StopShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        transform.localPosition = originalPosition;
    }

    private IEnumerator ShakeCoroutine(float shakeDuration)
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            ShakePosition();

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
        shakeCoroutine = null;
    }

    private IEnumerator ShakeLoopCoroutine()
    {
        while (true)
        {
            ShakePosition();

            yield return null;
        }
    }

    private void ShakePosition()
    {
        Vector2 offset = Random.insideUnitCircle * strength;

        transform.localPosition = originalPosition + new Vector3(
            offset.x,
            offset.y,
            0f
        );
    }
}
