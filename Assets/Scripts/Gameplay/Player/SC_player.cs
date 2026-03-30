using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class SC_player : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public float maxJumpTime = 0.3f;
    private float jumpTimeCounter;
    private bool isJumping;

    [Header("Health")]
    public int maxHealth = 3;
    private int currentHealth;
    public float hitFreezeTime = 0.15f;
    public Vector2 hitKnockback = new Vector2(5f, 3f); // force du knockback

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Damage Detection")]
    public Transform damageCheck;
    public float damageRadius = 0.5f;
    public LayerMask damageLayer;
    private bool canTakeDamage = true;

    [Header("Invincibility")]
    public float invincibilityTime = 1f; // Durée de l'invincibilité
    public SpriteRenderer spriteRenderer; // Assigne le sprite du joueur dans l'inspecteur
    private bool isInvincible = false;

    private Rigidbody2D rb;
    public Animator anim;

    private Vector2 moveInput;
    private bool isGrounded;
    private bool wasGrounded;
    private bool isFrozen;

    [Header("Inputs")]
    public InputActionReference Jump;
    public InputActionReference Move;

    [Header("Screen Wrap")]
    public float leftLimit = -10f;
    public float rightLimit = 10f;
    public Transform ghost;

    [Header("Double Jump")]
    public bool canDoubleJump = false; // Permet d'activer/désactiver le double saut
    private int jumpCount; // Compteur de sauts effectués

    private float levelWidth;
    private Coroutine hitCoroutine;

    private Vector2 knockbackVelocity; // stocke le knockback
    public ParticleSystem ps_damage;

    private SC_icecream_eat_system eat_system;
    void OnEnable()
    {
        Jump.action.Enable();
        Jump.action.performed += OnJumpStarted;
        Jump.action.canceled += OnJumpReleased;
    }

    void Start()
    {
        eat_system = SC_icecream_eat_system.instance;
        levelWidth = rightLimit - leftLimit;
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (!isFrozen)
            moveInput = Move.action.ReadValue<Vector2>();
        else
            moveInput = Vector2.zero;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        // Réinitialiser le compteur de sauts si le joueur est au sol
        if (isGrounded && !wasGrounded)
        {
            jumpCount = 0;
        }
        anim.SetBool("Run", Mathf.Abs(moveInput.x) > 0.1f);

        if (!wasGrounded && isGrounded && rb.linearVelocity.y <= 1)
        {
            anim.ResetTrigger("Jump");
            anim.SetTrigger("Land");
        }

        if (moveInput.x > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput.x < 0)
            transform.localScale = new Vector3(-1, 1, 1);

        wasGrounded = isGrounded;

        if (isJumping)
        {
            if (jumpTimeCounter > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpTimeCounter -= Time.deltaTime;
            }
            else
            {
                isJumping = false;
            }
        }

        CheckDamage();
    }

    void FixedUpdate()
    {
        if (!isFrozen)
        {
            // Mouvement horizontal + knockback horizontal
            float horizontalSpeed = moveInput.x * moveSpeed + knockbackVelocity.x;

            // Pour le vertical, on ne touche que si knockbackVelocity.y est significatif
            float verticalSpeed = rb.linearVelocity.y;
            if (knockbackVelocity.y != 0)
            {
                verticalSpeed = knockbackVelocity.y;
                knockbackVelocity.y = 0; // appliquer une seule fois
            }

            rb.linearVelocity = new Vector2(horizontalSpeed, verticalSpeed);

            // Diminution du knockback horizontal progressivement
            knockbackVelocity.x = Mathf.Lerp(knockbackVelocity.x, 0, 0.15f);
        }
    }

    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        if (!isFrozen)
        {
            if (isGrounded || (canDoubleJump && jumpCount < 2))
            {
                isJumping = true;
                jumpTimeCounter = maxJumpTime;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                anim.SetTrigger("Jump");

                jumpCount++; // Incrémente le compteur de sauts
            }
        }
    }

    private void OnJumpReleased(InputAction.CallbackContext context)
    {
        isJumping = false;
    }

    void LateUpdate()
    {
        float x = transform.position.x;

        if (x > (rightLimit - levelWidth / 2))
        {
            ghost.gameObject.SetActive(true);
            ghost.position = new Vector3(x - levelWidth, transform.position.y, transform.position.z);
        }
        else if (x < (leftLimit + levelWidth / 2))
        {
            ghost.gameObject.SetActive(true);
            ghost.position = new Vector3(x + levelWidth, transform.position.y, transform.position.z);
        }
        else
        {
            ghost.gameObject.SetActive(false);
        }

        if (x > rightLimit)
            transform.position = new Vector3(leftLimit, transform.position.y, transform.position.z);
        else if (x < leftLimit)
            transform.position = new Vector3(rightLimit, transform.position.y, transform.position.z);
    }

    void CheckDamage()
    {
        if (!canTakeDamage) return;

        Collider2D hit = Physics2D.OverlapCircle(damageCheck.position, damageRadius, damageLayer);
        if (hit != null)
        {
            TakeDamage(1, hit.transform.position);
        }
    }

    public void TakeDamage(int damage, Vector3 sourcePosition)
    {
        if (isFrozen || isInvincible) return; // Ignore les dégâts si gelé ou invincible
        ps_damage.Play();
        currentHealth -= damage;
        eat_system.take_damage();
        if (currentHealth > 0)
        {
            if (hitCoroutine != null) StopCoroutine(hitCoroutine);
            hitCoroutine = StartCoroutine(HitFreezeWithKnockback(sourcePosition));

            // Lancer l’invincibilité avec clignotement
            StartCoroutine(InvincibilityCoroutine());
        }
        else
        {
            Die();
        }
    }
    private IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        float elapsed = 0f;
        bool visible = true;

        while (elapsed < invincibilityTime)
        {
            elapsed += 0.1f; // incrément fixe, pas DeltaTime
            visible = !visible;
            spriteRenderer.enabled = visible;
            yield return new WaitForSecondsRealtime(0.1f); // temps réel
        }

        spriteRenderer.enabled = true;
        isInvincible = false;
    }
    private IEnumerator HitFreezeWithKnockback(Vector3 sourcePosition)
    {
        isFrozen = true;
        canTakeDamage = false;
        anim.SetTrigger("Hit");

        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = true;

        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(hitFreezeTime);

        rb.isKinematic = false;
        Time.timeScale = originalTimeScale;
        isFrozen = false;

        // Appliquer le knockback
        Vector2 direction = (transform.position - sourcePosition).normalized;
        knockbackVelocity = new Vector2(direction.x * hitKnockback.x, hitKnockback.y);

        yield return new WaitForSeconds(0.1f);
        canTakeDamage = true;
    }

    private void Die()
    {
        if (hitCoroutine != null) StopCoroutine(hitCoroutine);

        isFrozen = true;
        anim.SetTrigger("Die");
        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = false; // débloquer le Rigidbody
        knockbackVelocity = Vector2.zero;
        Time.timeScale = 0.5f; // temps ralenti
    }

    void OnDrawGizmosSelected()
    {
        if (damageCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(damageCheck.position, damageRadius);
        }
    }
}