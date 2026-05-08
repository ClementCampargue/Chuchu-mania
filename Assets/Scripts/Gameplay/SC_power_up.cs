using UnityEngine;

public class SC_power_up : MonoBehaviour
{
    [Header("Player")]
    public float detectionRadius = 0.5f;
    public LayerMask playerLayer;

    [Header("Ground")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.1f;
    public float hoverHeight = 0.15f;
    public float bounceForce = 2f;

    [Header("Score")]
    public int score_bonus;

    private bool isAttracted = false;
    private Transform attractTarget;
    private float attractSpeed;

    private Rigidbody2D rb;
    private bool grounded = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Attraction vers le joueur
        if (isAttracted && attractTarget != null)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                attractTarget.position,
                attractSpeed * Time.deltaTime
            );
        }

        // Détection joueur
        Collider2D hit = Physics2D.OverlapCircle(
            transform.position,
            detectionRadius,
            playerLayer
        );

        if (hit != null)
        {
            Collect();
        }

        // Détection du sol sans collider
        if (!grounded)
        {
            CheckGround();
        }
    }

    void CheckGround()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        if (hit.collider != null)
        {
            grounded = true;

            // Petit rebond
            rb.linearVelocity = Vector2.up * bounceForce;

            Invoke(nameof(DisablePhysics), 0.15f);
        }
    }

    void DisablePhysics()
    {
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        // Place légèrement au-dessus du sol
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            2f,
            groundLayer
        );

        if (hit.collider != null)
        {
            Vector3 pos = transform.position;
            pos.y = hit.point.y + hoverHeight;
            transform.position = pos;
        }
    }

    public void AttractTo(Transform target, float speed)
    {
        isAttracted = true;
        attractTarget = target;
        attractSpeed = speed;

        rb.simulated = false;
    }

    void Collect()
    {
        SC_score.Instance.score += score_bonus;
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            transform.position,
            transform.position + Vector3.down * groundCheckDistance
        );
    }
}