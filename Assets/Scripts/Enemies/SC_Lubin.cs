using System.Collections;
using UnityEngine;

public class SC_Lubin : MonoBehaviour
{
    [Header("Cibles et vitesse")]
    public Transform player;
    public float moveSpeed = 4f;
    public float jumpForce = 12f;

    [Header("Détection des plateformes")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.1f;
    public float platformDetectDistance = 3f;
    public Vector3 platformCheckAhead = new Vector3(1f, 0f, 0f);

    [Header("Warp écran")]
    public float screenLeft = -10f;
    public float screenRight = 10f;

    [Header("Delay")]
    public float directionChangeDelay = 0.3f;

    [Header("Animation")]
    public Animator anim;

    [Header("Anti-coincement")]
    public float minVerticalDistance = 1.0f;

    private Rigidbody2D rb;
    private bool isGrounded;
    private float directionTimer = 0f;
    private float currentDirection = 1f;
    private bool changingDirection = false;
    private bool wasGrounded = true;
    private bool jumped = false;
    private SC_icecream_eat_system eat;

    private bool forcedWalkMode = false; // Marche forcée pour tomber
    [Header("Knockback")]
    public float knockbackForce = 5f;
    public float knockbackVerticalForce = 5f;
    public float flickerDuration = 1f;
    public float flickerInterval = 0.1f;
    [Header("Player Detection")]
    public float detectionRadius = 0.5f;
    public LayerMask playerLayer;
    private bool isKnockedBack = false;
    private Vector2 knockbackVelocity;
    public Transform collision;
    public ParticleSystem ps;
    public SpriteRenderer spriteRenderer;
    public SC_juiciness juice; // ou ton type exact si c’est un script spécifique
    void Start()
    {
        player = SC_player.instance.transform;
        eat = SC_icecream_eat_system.instance;

        rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponent<Animator>();
    }

    void Update()
    {
   
        if (isKnockedBack)
        {
            rb.linearVelocity = knockbackVelocity;
            return;
        }
        if (SC_freeze_screen.freeze)
        {
            anim.speed = 0;
            return;
        }
        else
        {
            anim.speed = 1;

        }
        if (player == null) return;
        DetectPlayerOverlap();

        // Détection sol
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);

        // Atterrissage
        if (isGrounded && !wasGrounded)
        {
            anim.SetTrigger("Land");
            jumped = false;
            forcedWalkMode = false; // Arrête la marche forcée quand il atterrit
        }

        // Chute libre
        if (!isGrounded && wasGrounded && !jumped)
        {
            anim.SetTrigger("Fall");
        }

        wasGrounded = isGrounded;

        // --- Calcul distance minimale avec warp ---
        float screenWidth = screenRight - screenLeft;
        float dx = player.position.x - transform.position.x;

        float distDirect = Mathf.Abs(dx);
        float distLeftWarp = Mathf.Abs((player.position.x + screenWidth) - transform.position.x);
        float distRightWarp = Mathf.Abs((player.position.x - screenWidth) - transform.position.x);

        float minDist = Mathf.Min(distDirect, distLeftWarp, distRightWarp);

        Vector3 targetPos = player.position;
        if (minDist == distLeftWarp) targetPos.x = player.position.x + screenWidth;
        if (minDist == distRightWarp) targetPos.x = player.position.x - screenWidth;

        // --- Anti-coincement : si le joueur est sous l'ennemi ---
        bool playerBelow = player.position.y + 0.2f < transform.position.y - minVerticalDistance;
        if (playerBelow && isGrounded)
        {
            forcedWalkMode = true; // Active la marche forcée
        }

        // Direction cible
        float targetDirection = Mathf.Sign(targetPos.x - transform.position.x);

        // Changement de direction uniquement au sol
        if (isGrounded && !forcedWalkMode)
        {
            if (targetDirection != currentDirection)
            {
                changingDirection = true;
                directionTimer += Time.deltaTime;

                if (directionTimer >= directionChangeDelay)
                {
                    currentDirection = targetDirection;
                    directionTimer = 0f;
                    changingDirection = false;
                    anim.ResetTrigger("Fall");
                    anim.ResetTrigger("Land");
                    anim.SetTrigger("Turn");
                }
            }
            else
            {
                directionTimer = 0f;
                changingDirection = false;
            }
        }

        // Déplacement horizontal
        float horizontalVelocity = (changingDirection ? 0f : currentDirection * moveSpeed);

        // Si mode marche forcée, avance même si pas de plateforme
        if (forcedWalkMode)
        {
            rb.linearVelocity = new Vector2(currentDirection * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(horizontalVelocity, rb.linearVelocity.y);
        }

        // Trigger Run
        anim.SetBool("Run", (!changingDirection && isGrounded) || forcedWalkMode);

        // Platform check en avant
        Vector3 offset = new Vector3(platformCheckAhead.x * currentDirection, platformCheckAhead.y, platformCheckAhead.z);
        bool willLandOnPlatform = Physics2D.Raycast(
            transform.position + offset,
            Vector2.down,
            platformDetectDistance,
            groundLayer
        );

        // Saut intelligent
        if (!forcedWalkMode && isGrounded && !changingDirection && (player.position.y > transform.position.y + 0.5f) && willLandOnPlatform)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            anim.SetTrigger("Jump");
            jumped = true;
        }

        // Warp réel
        if (transform.position.x < screenLeft) transform.position = new Vector3(screenRight, transform.position.y, transform.position.z);
        if (transform.position.x > screenRight) transform.position = new Vector3(screenLeft, transform.position.y, transform.position.z);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);

        if (player != null)
        {
            Vector3 offset = new Vector3(platformCheckAhead.x * currentDirection, platformCheckAhead.y, platformCheckAhead.z);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position + offset,
                            transform.position + offset + Vector3.down * platformDetectDistance);

            // Visualisation warp
            float screenWidth = screenRight - screenLeft;
            Vector3 warpClone = player.position;
            float dx = player.position.x - transform.position.x;
            float distDirect = Mathf.Abs(dx);
            float distLeftWarp = Mathf.Abs((player.position.x + screenWidth) - transform.position.x);
            float distRightWarp = Mathf.Abs((player.position.x - screenWidth) - transform.position.x);
            float minDist = Mathf.Min(distDirect, distLeftWarp, distRightWarp);

            if (minDist == distLeftWarp) warpClone.x = player.position.x + screenWidth;
            if (minDist == distRightWarp) warpClone.x = player.position.x - screenWidth;

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(warpClone, 0.3f);
        }
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(collision.transform.position, detectionRadius);

    }
    private void DetectPlayerOverlap()
    {
        Collider2D playerCollider = Physics2D.OverlapCircle(collision.transform.position, detectionRadius, playerLayer);
        if (playerCollider != null && eat.isPowerUpActive)
        {
            bool playerOnRight = playerCollider.transform.position.x > transform.position.x;
            Knockback(playerOnRight);
        }
    }

    public void Knockback(bool playerOnRight)
    {
        if (ps != null) ps.Play();

        if (isKnockedBack) return;

        if (juice != null)
        {
            // si ton script juice a une méthode spécifique, adapte ici
            juice.SendMessage("PlayJuice", SendMessageOptions.DontRequireReceiver);
        }

        isKnockedBack = true;

        // Désactiver collisions
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Direction du knockback
        float horizontalDir = playerOnRight ? -1f : 1f;
        knockbackVelocity = new Vector2(horizontalDir * knockbackForce, knockbackVerticalForce);

        StartCoroutine(FlickerAndDestroy());
    }

    private IEnumerator FlickerAndDestroy()
    {
        anim.enabled = false;
        float elapsed = 0f;

        while (elapsed < flickerDuration)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = !spriteRenderer.enabled;

            yield return new WaitForSeconds(flickerInterval);
            elapsed += flickerInterval;
        }

        Destroy(gameObject);
    }
}