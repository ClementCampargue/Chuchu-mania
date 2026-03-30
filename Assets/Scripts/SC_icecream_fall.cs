using UnityEngine;

public class SC_icecream_fall : MonoBehaviour
{
    public Transform targetPosition;   // Où l'objet doit tomber
    public float gravity = 9.8f;       // Intensité de la gravité simulée

    public float punchScaleAmount = 1.2f; // Combien l'objet s'agrandit
    public float punchDuration = 0.2f;    // Durée de l'effet

    public float bounceForce = 5f;        // Force du saut horizontal
    public float blinkDuration = 2f;      // Durée totale du clignotement avant désactivation
    public float blinkInterval = 0.1f;    // Intervalle de clignotement

    private Vector3 velocity = Vector3.zero;
    private bool hasLanded = false;
    public SpriteRenderer spriteRenderer;

    void Awake()
    {
    }

    void Update()
    {
        if (!hasLanded)
        {
            // Calcul de la descente
            velocity.y -= gravity * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;

            // Vérifie si l'objet a atteint ou dépassé la position cible
            if (transform.position.y <= targetPosition.position.y)
            {
                transform.position = new Vector3(transform.position.x, targetPosition.position.y, transform.position.z);
                hasLanded = true;
                StartCoroutine(JuicyEffect());
            }
        }
    }

    System.Collections.IEnumerator JuicyEffect()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 punchScale = originalScale * punchScaleAmount;

        float elapsed = 0f;

        // Agrandissement rapide
        while (elapsed < punchDuration / 2)
        {
            transform.localScale = Vector3.Lerp(originalScale, punchScale, elapsed / (punchDuration / 2));
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Retour à la taille originale
        elapsed = 0f;
        while (elapsed < punchDuration / 2)
        {
            transform.localScale = Vector3.Lerp(punchScale, originalScale, elapsed / (punchDuration / 2));
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    public void Eat()
    {
        Destroy(gameObject);
    }

    public void BounceAndBlink()
    {
        StartCoroutine(BounceAndBlinkCoroutine());
    }

    System.Collections.IEnumerator BounceAndBlinkCoroutine()
    {
        // Direction aléatoire sur X
        float randomDir = Random.Range(-1f, 1f);
        velocity = new Vector3(randomDir * bounceForce, bounceForce, 0f);

        float elapsed = 0f;
        bool visible = true;

        while (elapsed < blinkDuration)
        {
            // Applique la gravité
            velocity.y -= gravity * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;

            // Clignotement
            elapsed += Time.deltaTime;
            if (elapsed % blinkInterval < blinkInterval / 2)
                visible = true;
            else
                visible = false;

            spriteRenderer.enabled = visible;

            yield return null;
        }

        // Désactivation
        gameObject.SetActive(false);
    }
}