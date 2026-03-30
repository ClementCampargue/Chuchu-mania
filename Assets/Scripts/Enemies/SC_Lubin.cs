using UnityEngine;

public class SC_Lubin : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float maxJumpForce = 7f;
    public float chaseRange = 10f;

    [Header("Stun")]
    public float stopTimeOnHit = 2f;

    [Header("Detection")]
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundCheckDistance = 1f;
    public float platformLookDistance = 5f; // distance maximale pour détecter les plateformes
    public float maxJumpHeight = 2f;        // hauteur maximale qu'il peut sauter

    private Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;
    private GameObject player;
    private bool isStunned = false;
    private float stunTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void FixedUpdate()
    {
        if (isStunned)
        {
            stunTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (stunTimer <= 0) isStunned = false;
            return;
        }

        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.transform.position);
        if (distance <= chaseRange)
        {
            float direction = Mathf.Sign(player.transform.position.x - transform.position.x);
            spriteRenderer.flipX = direction < 0;

            // Détection du sol devant l'ennemi
            bool isGroundAhead = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);

            // Détection d'une plateforme devant pour sauter
            RaycastHit2D platformHit = Physics2D.Raycast(groundCheck.position + Vector3.up * 0.5f, Vector2.right * direction, platformLookDistance, groundLayer);

            if (platformHit.collider != null)
            {
                float platformHeight = platformHit.point.y;
                float heightDiff = platformHeight - transform.position.y;

                if (heightDiff > 0.1f && heightDiff <= maxJumpHeight)
                {
                    // Calcul du jump vertical nécessaire
                    float jumpForce = Mathf.Min(maxJumpForce, Mathf.Sqrt(2 * 9.81f * heightDiff));
                    rb.linearVelocity = new Vector2(direction * moveSpeed, jumpForce);
                }
                else if (isGroundAhead)
                {
                    rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
                }
                else
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // stop au bord
                }
            }
            else if (isGroundAhead)
            {
                rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
            }
            else
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // stop au bord
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            Vector2 contactPoint = collision.contacts[0].point;
            if (contactPoint.y > transform.position.y + 0.2f)
            {
                StunEnemy();
            }
        }
    }

    void StunEnemy()
    {
        isStunned = true;
        stunTimer = stopTimeOnHit;
        rb.linearVelocity = Vector2.zero;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundCheckDistance);
            Gizmos.DrawLine(groundCheck.position + Vector3.up * 0.5f, groundCheck.position + Vector3.up * 0.5f + Vector3.right * platformLookDistance);
        }
    }
}