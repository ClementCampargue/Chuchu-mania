
using UnityEngine;
using System.Collections;

public class SC_power_up_fill : MonoBehaviour
{
    [SerializeField] private float duration = 2f;
    [SerializeField] private float delay = 1f;

    private Vector3 initialScale;
    private Coroutine scaleCoroutine;

    private void Awake()
    {
        initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        transform.localScale = initialScale;

        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);

        scaleCoroutine = StartCoroutine(ScaleAnimation());
    }

    private IEnumerator ScaleAnimation()
    {
        float elapsed = 0f;
        yield return new WaitForSeconds(delay);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            transform.localScale = new Vector3(
                Mathf.Lerp(initialScale.x, 0f, t),
                initialScale.y,
                initialScale.z
            );

            yield return null;
        }

        transform.localScale = new Vector3(
            0f,
            initialScale.y,
            initialScale.z
        );

        scaleCoroutine = null;
    }
}
