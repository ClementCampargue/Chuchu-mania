using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SC_enemy_track_player : MonoBehaviour
{
    [Header("Tracking")]
    [SerializeField] private float speed = 3f;

    [Tooltip("Force avec laquelle l'ennemi change sa direction.")]
    [SerializeField] private float linearity = 5f;

    [Header("Movement Amplitude")]
    [Tooltip("Amplitude du mouvement latéral autour de la direction du joueur.")]
    [SerializeField] private float amplitude = 0.5f;

    [Tooltip("Vitesse de l'oscillation.")]
    [SerializeField] private float amplitudeFrequency = 3f;

    [Header("Flip")]
    [SerializeField] private bool flipX = true;

    [Header("Tracking ON/OFF")]
    [SerializeField] private bool tracking = false;

    private Rigidbody2D rb;
    private Transform player;

    private float originalScaleX;

    public Animator anim;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalScaleX = Mathf.Abs(transform.localScale.x);
    }

    void Start()
    {
        if (SC_player.instance != null)
        {
            player = SC_player.instance.transform;
        }

        // État initial
        if (anim != null)
        {
            anim.SetBool("on", tracking);
        }
    }

    /// <summary>
    /// Active le tracking du joueur.
    /// </summary>
    public void start_tracking()
    {
        tracking = true;

        if (anim != null)
        {
            anim.SetBool("on", true);
        }
    }

    /// <summary>
    /// Désactive le tracking du joueur.
    /// </summary>
    public void end_tracking()
    {
        tracking = false;

        // Arrête immédiatement le déplacement
        rb.linearVelocity = Vector2.zero;

        if (anim != null)
        {
            anim.SetBool("on", false);
        }
    }

    void FixedUpdate()
    {
        // Si le tracking est OFF, on ne fait rien
        if (!tracking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Recherche du joueur si nécessaire
        if (player == null)
        {
            if (SC_player.instance != null)
            {
                player = SC_player.instance.transform;
            }

            return;
        }

        // Direction vers le joueur
        Vector2 direction =
            ((Vector2)player.position - rb.position).normalized;

        // Flip
        if (flipX)
        {
            FlipTowardsPlayer();
        }

        // Perpendiculaire à la direction
        Vector2 perpendicular =
            new Vector2(-direction.y, direction.x);

        // Oscillation latérale
        float wave =
            Mathf.Sin(Time.time * amplitudeFrequency) * amplitude;

        // Direction finale
        Vector2 targetDirection =
            (direction + perpendicular * wave).normalized;

        // Vitesse cible
        Vector2 targetVelocity =
            targetDirection * speed;

        // Transition vers la vitesse cible
        rb.linearVelocity = Vector2.Lerp(
            rb.linearVelocity,
            targetVelocity,
            linearity * Time.fixedDeltaTime
        );
    }

    private void FlipTowardsPlayer()
    {
        float difference =
            player.position.x - transform.position.x;

        // Joueur à droite
        if (difference > 0.05f)
        {
            transform.localScale = new Vector3(
                originalScaleX,
                transform.localScale.y,
                transform.localScale.z
            );
        }
        // Joueur à gauche
        else if (difference < -0.05f)
        {
            transform.localScale = new Vector3(
                -originalScaleX,
                transform.localScale.y,
                transform.localScale.z
            );
        }
    }
}
