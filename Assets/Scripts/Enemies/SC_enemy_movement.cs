using UnityEngine;
using System.Collections;

public class SC_enemy_movement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public Transform groundDetection;
    public float detectionDistance = 1f;
    public float flipCooldown = 0.2f;

    [Header("Knockback")]
    public ParticleSystem ps;
    public Transform visuals;
    public float knockbackForce = 5f;
    public float knockbackVerticalForce = 5f;
    public float spinSpeed = 360f;
    public float flickerDuration = 2f;
    public float flickerInterval = 0.1f;
    public float gravity = 9.8f;

    [Header("Player Detection")]
    public float detectionRadius = 0.5f;
    public LayerMask playerLayer;

    [Header("Enemy Detection")]
    public float enemyDetectionDistance = 0.2f;
    public LayerMask enemyLayer;

    [Header("References")]
    public SpriteRenderer spriteRenderer;
    public LayerMask groundLayer;
    public SC_juiciness juice;
    public Transform collision;

    private bool movingRight = true;
    private float lastFlipTime = 0f;

    private SC_player player;
    private SC_icecream_eat_system eat;

    private bool isKnockedBack = false;
    private Vector2 knockbackVelocity = Vector2.zero;

    void Start()
    {
        player = SC_player.instance;
        eat = SC_icecream_eat_system.instance;
    }

    void Update()
    {
        if (!SC_level_intro.gameStarted)
            return;

        if (!isKnockedBack)
        {
            DetectPlayerOverlap();

            if (SC_freeze_screen.freeze)
                return;

            HandleMovement();
        }
        else
        {
            HandleKnockback();
        }
    }

    void HandleMovement()
    {
        // Déplacement
        transform.position +=
            (movingRight ? Vector3.right : Vector3.left)
            * moveSpeed
            * Time.deltaTime;

        // Vérifie si il y a du sol devant
        bool isGroundAhead =
            Physics2D.Raycast(
                groundDetection.position,
                Vector2.down,
                detectionDistance,
                groundLayer
            );

        // Vérifie si il y a un mur devant
        bool isWallAhead =
            Physics2D.Raycast(
                groundDetection.position,
                movingRight ? Vector2.right : Vector2.left,
                0.1f,
                groundLayer
            );

        // Vérifie si un autre ennemi est devant
        RaycastHit2D enemyHit =
            Physics2D.Raycast(
                groundDetection.position,
                movingRight ? Vector2.right : Vector2.left,
                enemyDetectionDistance,
                enemyLayer
            );

        bool isEnemyAhead =
            enemyHit.collider != null
            && enemyHit.collider.gameObject != gameObject;

        // Change de direction
        if ((!isGroundAhead || isWallAhead || isEnemyAhead)
            && Time.time - lastFlipTime > flipCooldown)
        {
            Flip();
            lastFlipTime = Time.time;
        }
    }

    void HandleKnockback()
    {
        knockbackVelocity.y -= gravity * Time.deltaTime;

        transform.position +=
            (Vector3)(knockbackVelocity * Time.deltaTime);

        visuals.Rotate(
            Vector3.forward * spinSpeed * Time.deltaTime
        );
    }

    void DetectPlayerOverlap()
    {
        Collider2D playerCollider =
            Physics2D.OverlapCircle(
                collision.position,
                detectionRadius,
                playerLayer
            );

        if (playerCollider != null && eat.isPowerUpActive)
        {
            bool playerOnRight =
                playerCollider.transform.position.x
                > transform.position.x;

            Knockback(playerOnRight);
        }
    }

    public void Flip()
    {
        movingRight = !movingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    public void Knockback(bool playerOnRight)
    {
        if (isKnockedBack)
            return;

        ps.Play();
        juice.PlayJuice();

        isKnockedBack = true;

        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
            col.enabled = false;

        float horizontalDir =
            playerOnRight ? -1f : 1f;

        knockbackVelocity = new Vector2(
            horizontalDir * knockbackForce,
            knockbackVerticalForce
        );

        StartCoroutine(FlickerAndDestroy());
    }

    IEnumerator FlickerAndDestroy()
    {
        float elapsed = 0f;

        while (elapsed < flickerDuration)
        {
            spriteRenderer.enabled =
                !spriteRenderer.enabled;

            yield return new WaitForSeconds(
                flickerInterval
            );

            elapsed += flickerInterval;
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        if (groundDetection != null)
        {
            Gizmos.color = Color.red;

            // Sol
            Gizmos.DrawLine(
                groundDetection.position,
                groundDetection.position
                + Vector3.down * detectionDistance
            );

            // Mur
            Gizmos.DrawLine(
                groundDetection.position,
                groundDetection.position
                + (movingRight
                    ? Vector3.right
                    : Vector3.left) * 0.1f
            );

            // Ennemi
            Gizmos.color = Color.yellow;

            Gizmos.DrawLine(
                groundDetection.position,
                groundDetection.position
                + (movingRight
                    ? Vector3.right
                    : Vector3.left)
                    * enemyDetectionDistance
            );
        }

        if (collision != null)
        {
            Gizmos.color = Color.blue;

            Gizmos.DrawWireSphere(
                collision.position,
                detectionRadius
            );
        }
    }
}