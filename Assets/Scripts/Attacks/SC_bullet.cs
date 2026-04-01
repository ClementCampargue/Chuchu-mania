using UnityEngine;

public class SC_bullet : MonoBehaviour
{
    private Transform target;          // Le joueur
    public float speed = 10f;          // Vitesse du projectile
    public bool predictMovement;       // Bool pour activer l'anticipation
    public float predictionFactor = 1f; // Ajustement de l'anticipation

    private Vector2 velocity;

    void Start()
    {
        target = SC_player.instance.transform;

        Vector2 aimPosition = target.position;

        if (predictMovement)
        {
            Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
            if (targetRb != null)
            {
                float distance = Vector2.Distance(transform.position, target.position);
                float timeToReach = distance / speed;

                aimPosition = (Vector2)target.position + targetRb.linearVelocity * timeToReach * predictionFactor;
            }
        }

        // Calcul de la direction
        Vector2 direction = (aimPosition - (Vector2)transform.position).normalized;
        velocity = direction * speed;

        // Orientation du projectile en 2D
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f); // -90 si le sprite pointe vers le haut
    }

    void Update()
    {
        transform.position += (Vector3)velocity * Time.deltaTime;
    }
}