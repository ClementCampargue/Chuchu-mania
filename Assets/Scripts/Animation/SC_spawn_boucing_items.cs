using UnityEngine;
using System.Collections;

public class SC_spawn_boucing_items : MonoBehaviour
{
    [Header("Objets à éjecter")]
    public GameObject gb;

    [Header("Paramètres")]
    public float minForce = 3f;
    public float maxForce = 8f;
    public float upwardForce = 5f;
    public float lifetime = 5f;

    [Header("Effet Flicker")]
    public float flickerDuration = 1.5f;
    public float flickerInterval = 0.1f;

    public void EjectRings(int amount)
    {
        if (gb == null)
            return;

        for (int i = 0; i < amount; i++)
        {
            GameObject gb_ = Instantiate(
                gb,
                transform.position,
                Quaternion.identity
            );

            // Direction et vitesse initiales
            float angle = Random.Range(0f, 360f);

            Vector2 direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            float force = Random.Range(minForce, maxForce);

            Vector2 initialVelocity =
                direction * force + Vector2.up * upwardForce;

            // Mouvement indépendant du Time.timeScale
            SC_UnscaledMovement movement =
                gb_.GetComponent<SC_UnscaledMovement>();

            if (movement == null)
                movement = gb_.AddComponent<SC_UnscaledMovement>();

            movement.Initialize(initialVelocity);

            // Clignotement en temps réel
            SC_ItemFlicker flicker =
                gb_.GetComponent<SC_ItemFlicker>();

            if (flicker == null)
                flicker = gb_.AddComponent<SC_ItemFlicker>();

            flicker.StartFlicker(
                flickerDuration,
                flickerInterval
            );

            // Destruction en temps réel
            SC_UnscaledLifetime lifetimeComponent =
                gb_.AddComponent<SC_UnscaledLifetime>();

            lifetimeComponent.Initialize(lifetime);
        }
    }
}

public class SC_ItemFlicker : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Coroutine flickerCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void StartFlicker(float duration, float interval)
    {
        if (flickerCoroutine != null)
            StopCoroutine(flickerCoroutine);

        flickerCoroutine = StartCoroutine(
            FlickerRoutine(duration, interval)
        );
    }

    private IEnumerator FlickerRoutine(float duration, float interval)
    {
        if (spriteRenderer == null)
            yield break;

        float timer = 0f;
        bool visible = true;

        while (timer < duration)
        {
            visible = !visible;
            spriteRenderer.enabled = visible;

            yield return new WaitForSecondsRealtime(interval);
            timer += interval;
        }

        spriteRenderer.enabled = true;
        flickerCoroutine = null;
    }
}

public class SC_UnscaledMovement : MonoBehaviour
{
    [Header("Mouvement")]
    public Vector2 velocity;
    public float gravity = 9.81f;

    public void Initialize(Vector2 initialVelocity)
    {
        velocity = initialVelocity;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Gravité manuelle
        velocity += Vector2.down * gravity * dt;

        // Déplacement indépendant du freeze frame
        transform.position += (Vector3)(velocity * dt);
    }
}

public class SC_UnscaledLifetime : MonoBehaviour
{
    public void Initialize(float duration)
    {
        StartCoroutine(DestroyAfterRealtime(duration));
    }

    private IEnumerator DestroyAfterRealtime(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);

        Destroy(gameObject);
    }
}
