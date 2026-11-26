
using System.Collections;
using UnityEngine;

public class SC_low_health_effect : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Coroutine lowHealthCoroutine;
    private sc_health_system health;

    [Header("Low Health Warning")]
    private Material normalMaterial;
    public Material lowHealthMaterial;
    public float lowHealthBlinkRate = 0.2f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            normalMaterial = spriteRenderer.material;
    }

    private void Start()
    {
        health = sc_health_system.instance;
        CheckLowHealth();
    }

    private void OnEnable()
    {
        if (spriteRenderer != null)
            CheckLowHealth();
    }

    private void OnDisable()
    {
        if (lowHealthCoroutine != null)
        {
            StopCoroutine(lowHealthCoroutine);
            lowHealthCoroutine = null;
        }
    }

    private void Update()
    {
        CheckLowHealth();
    }

    private void CheckLowHealth()
    {
        if (health == null)
            health = sc_health_system.instance;

        if (health == null || spriteRenderer == null)
            return;

        if (health.current_health == 1)
        {
            if (lowHealthCoroutine == null)
                lowHealthCoroutine = StartCoroutine(LowHealthBlink());
        }
        else
        {
            if (lowHealthCoroutine != null)
            {
                StopCoroutine(lowHealthCoroutine);
                lowHealthCoroutine = null;
            }

            spriteRenderer.material = normalMaterial;
        }
    }

    private IEnumerator LowHealthBlink()
    {
        bool toggle = false;

        while (health != null && health.current_health == 1)
        {
            spriteRenderer.material = toggle
                ? normalMaterial
                : lowHealthMaterial;

            toggle = !toggle;

            yield return new WaitForSeconds(lowHealthBlinkRate);
        }

        spriteRenderer.material = normalMaterial;
        lowHealthCoroutine = null;
    }
}
