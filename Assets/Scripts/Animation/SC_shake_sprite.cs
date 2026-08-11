using System.Collections;
using UnityEngine;

public class SpriteShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float strength = 0.1f;
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private float frequency = 20f;

    [Header("Loop")]
    [SerializeField] private bool loop = false;
    [SerializeField] private bool onStart = false;

    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        originalPosition = transform.localPosition;
    }

    private void Start()
    {
        if (onStart)
        {
            if (loop)
                StartLoop();
            else
                Shake();
        }
    }

    public void Shake()
    {
        Shake(duration);
    }

    public void Shake(float customDuration)
    {
        StopShake();

        shakeCoroutine = StartCoroutine(ShakeCoroutine(customDuration));
    }

    public void StartLoop()
    {
        StopShake();

        shakeCoroutine = StartCoroutine(ShakeLoopCoroutine());
    }

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
        float timer = 0f;
        float interval = 1f / Mathf.Max(frequency, 0.01f);

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            timer += Time.deltaTime;

            if (timer >= interval)
            {
                timer -= interval;
                ShakePosition();
            }

            yield return null;
        }

        transform.localPosition = originalPosition;
        shakeCoroutine = null;
    }

    private IEnumerator ShakeLoopCoroutine()
    {
        float timer = 0f;
        float interval = 1f / Mathf.Max(frequency, 0.01f);

        while (true)
        {
            timer += Time.deltaTime;

            if (timer >= interval)
            {
                timer -= interval;
                ShakePosition();
            }

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
