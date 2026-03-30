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
    public SC_juiciness juice;
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
                juice.PlayJuice();
            }
        }
    }


    public void Eat()
    {
        juice.PlayJuice();
        BounceAndBlink();
    }

    public void BounceAndBlink()
    {
        juice.PlayJuice();
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