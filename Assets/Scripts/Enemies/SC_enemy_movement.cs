using UnityEngine;
using System.Collections;

public class SC_enemy_movement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public Transform groundDetection;
    public float detectionDistance = 1f;
    public float flipCooldown = 0.2f;

    [Header("Screen Wrap")]
    public float leftLimit = -10f;
    public float rightLimit = 10f;
    public GameObject ghostPrefab;

    [Header("Knockback")]
    public ParticleSystem ps;
    public Transform visuals;
    public float knockbackForce = 5f;
    public float knockbackVerticalForce = 5f;
    public float spinSpeed = 360f;
    public float flickerDuration = 2f;
    public float flickerInterval = 0.1f;
    public float gravity = 9.8f; // gravité simulée

    [Header("Player Detection")]
    public float detectionRadius = 0.5f;
    public LayerMask playerLayer;

    private bool movingRight = true;
    private float lastFlipTime = 0f;
    public SpriteRenderer spriteRenderer;
    public LayerMask groundLayer;

    private GameObject ghost;
    private float levelWidth;

    private SC_player player;
    private SC_icecream_eat_system eat;
    private bool isKnockedBack = false;

    // Variables pour knockback manuel
    private Vector2 knockbackVelocity = Vector2.zero;
    public SC_juiciness juice;
    public BoxCollider2D collision;
    void Start()
    {
        player = SC_player.instance;
        eat = SC_icecream_eat_system.instance;
        levelWidth = rightLimit - leftLimit;

        if (ghostPrefab != null)
        {
            ghost = Instantiate(ghostPrefab);
            ghost.SetActive(false);
        }
    }

    void Update()
    {
        if (!isKnockedBack)
        {
            HandleMovement();
            UpdateGhost();
            HandleScreenWrap();
            DetectPlayerOverlap();
        }
        else
        {
            // Appliquer knockback et gravité manuellement
            knockbackVelocity.y -= gravity * Time.deltaTime;
            transform.position += (Vector3)(knockbackVelocity * Time.deltaTime);
            visuals.Rotate(Vector3.forward * spinSpeed * Time.deltaTime);
        }
    }

    private void HandleMovement()
    {
        // Déplacement horizontal manuel
        transform.position += (movingRight ? Vector3.right : Vector3.left) * moveSpeed * Time.deltaTime;

        // Vérifie sol et mur
        bool isGroundAhead = Physics2D.Raycast(groundDetection.position, Vector2.down, detectionDistance, groundLayer);
        bool isWallAhead = Physics2D.Raycast(groundDetection.position, movingRight ? Vector2.right : Vector2.left, 0.1f, groundLayer);

        if ((!isGroundAhead || isWallAhead) && Time.time - lastFlipTime > flipCooldown)
        {
            Flip();
            lastFlipTime = Time.time;
        }
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

    void Flip()
    {
        movingRight = !movingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    void UpdateGhost()
    {
        if (ghost == null) return;

        Vector3 pos = transform.position;

        if (pos.x > rightLimit - levelWidth / 2)
        {
            ghost.SetActive(true);
            ghost.transform.position = new Vector3(pos.x - levelWidth, pos.y, pos.z);
            ghost.transform.localScale = transform.localScale;
        }
        else if (pos.x < leftLimit + levelWidth / 2)
        {
            ghost.SetActive(true);
            ghost.transform.position = new Vector3(pos.x + levelWidth, pos.y, pos.z);
            ghost.transform.localScale = transform.localScale;
        }
        else
        {
            ghost.SetActive(false);
        }
    }

    private void HandleScreenWrap()
    {
        Vector3 pos = transform.position;
        if (pos.x > rightLimit) pos.x = leftLimit;
        else if (pos.x < leftLimit) pos.x = rightLimit;
        transform.position = pos;
    }

    public void Knockback(bool playerOnRight)
    {
        ps.Play();
        if (isKnockedBack) return;
        collision.enabled = false;
        juice.PlayJuice();
        isKnockedBack = true;

        // Désactiver collisions si nécessaire
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Définir la vitesse initiale du knockback
        float horizontalDir = playerOnRight ? -1f : 1f;
        knockbackVelocity = new Vector2(horizontalDir * knockbackForce, knockbackVerticalForce);

        // Lancer flicker et destruction
        StartCoroutine(FlickerAndDestroy());
    }

    private IEnumerator FlickerAndDestroy()
    {       
        spriteRenderer.enabled = !spriteRenderer.enabled;

        float elapsed = 0f;
        while (elapsed < flickerDuration)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(flickerInterval);
            elapsed += flickerInterval;
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        if (groundDetection != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(groundDetection.position, groundDetection.position + Vector3.down * detectionDistance);
            Gizmos.DrawLine(groundDetection.position, groundDetection.position + (movingRight ? Vector3.right : Vector3.left) * 0.1f);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(collision.transform.position, detectionRadius);
    }
}