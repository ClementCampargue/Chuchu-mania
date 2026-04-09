using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class SC_player : MonoBehaviour
{
    [Header("Power_up_stats")]
    public float PowermoveSpeed = 5f;
    public float PowerJump = 5f;

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
    public Vector2 hitKnockback = new Vector2(5f, 3f);

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
    public float invincibilityTime = 1f;
    public SpriteRenderer spriteRenderer;
    private bool isInvincible = false;

    [Header("Transformation")]
    public float transformFreezeTime = 0.5f;
    public string transformAnimTrigger = "Transform";
    public string detransformAnimTrigger = "DeTransform";

    private Rigidbody2D rb;
    public Animator anim_;
    public Animator anim_powerup;
    private Animator anim;

    private Vector2 moveInput;
    public bool isGrounded;
    private bool wasGrounded;
    private bool isFrozen;

    [Header("Inputs")]
    public InputActionReference Jump;
    public InputActionReference Move;

    [Header("Screen Wrap")]
    public float leftLimit = -10f;
    public float rightLimit = 10f;
    public Transform ghost;

    private float levelWidth;
    private Coroutine hitCoroutine;
    private Vector2 knockbackVelocity;
    public ParticleSystem ps_damage;

    private SC_icecream_eat_system eat_system;
    public static SC_player instance;
    public BoxCollider2D collider;

    public GameObject game_over_screen;
    public bool canMove;
    private void Awake()
    {
        instance = this;
    }

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
        anim = anim_;
    }

    void Update()
    {
        if (!isFrozen  && canMove)
            moveInput = Move.action.ReadValue<Vector2>();
        else
            moveInput = Vector2.zero;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        anim.SetBool("Run", Mathf.Abs(moveInput.x) > 0.1f);

        if (!wasGrounded && isGrounded && rb.linearVelocity.y <= 1f)
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
                float jumpPower = eat_system.isPowerUpActive ? PowerJump : jumpForce;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
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
            float horizontalSpeed = moveInput.x * (eat_system.isPowerUpActive ? PowermoveSpeed : moveSpeed) + knockbackVelocity.x;

            float verticalSpeed = rb.linearVelocity.y;
            if (knockbackVelocity.y != 0)
            {
                verticalSpeed = knockbackVelocity.y;
                knockbackVelocity.y = 0;
            }

            rb.linearVelocity = new Vector2(horizontalSpeed, verticalSpeed);

            knockbackVelocity.x = Mathf.Lerp(knockbackVelocity.x, 0, 0.15f);
        }
    }

    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        if (!isFrozen && canMove)
        {
            if (isGrounded || eat_system.isPowerUpActive)
            {
                isJumping = true;
                jumpTimeCounter = maxJumpTime;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                anim.SetTrigger("Jump");
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
        if (isFrozen || isInvincible || eat_system.isPowerUpActive) return;

        ps_damage.Play();
        currentHealth -= damage;
        eat_system.take_damage();

        if (currentHealth > 0)
        {
            if (hitCoroutine != null) StopCoroutine(hitCoroutine);
            hitCoroutine = StartCoroutine(HitFreezeWithKnockback(sourcePosition));
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
            elapsed += 0.1f;
            visible = !visible;
            spriteRenderer.enabled = visible;
            yield return new WaitForSecondsRealtime(0.1f);
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
        rb.bodyType = RigidbodyType2D.Kinematic;

        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(hitFreezeTime);

        rb.bodyType = RigidbodyType2D.Dynamic;
        Time.timeScale = originalTimeScale;
        isFrozen = false;

        Vector2 direction = (transform.position - sourcePosition).normalized;
        knockbackVelocity = new Vector2(direction.x * hitKnockback.x, hitKnockback.y);

        yield return new WaitForSeconds(0.1f);
        canTakeDamage = true;
    }

    private IEnumerator PowerupFreeze(bool isActivating)
    {
        isFrozen = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        string trigger = isActivating ? transformAnimTrigger : detransformAnimTrigger;
        anim.SetTrigger(trigger);

        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(transformFreezeTime);

        Time.timeScale = originalTimeScale;
        rb.bodyType = RigidbodyType2D.Dynamic;
        isFrozen = false;
    }
    private void Die()
    {
        if (hitCoroutine != null) StopCoroutine(hitCoroutine);

        isFrozen = true;
        anim.SetBool("Die",true);


            collider.enabled = false;

        // Faire rebondir légèrement le joueur vers le haut
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = new Vector2(0, 5f); // rebond initial
        rb.gravityScale = 1f;

        knockbackVelocity = Vector2.zero;

        // Ralentir le temps pour effet dramatique
        Time.timeScale = 0.5f;

        // Commencer coroutine pour la chute après le rebond
        StartCoroutine(FallAfterBounce());
    }

    private IEnumerator FallAfterBounce()
    {
        // Attendre que le rebond atteigne son apogée
        yield return new WaitForSecondsRealtime(0.3f);
        game_over_screen.SetActive(true);
        // Lancer la chute vers le bas
        rb.linearVelocity = new Vector2(0, -5f);
    }
    void OnDrawGizmosSelected()
    {
        if (damageCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(damageCheck.position, damageRadius);
        }
    }

    public void powerup()
    {
        anim_powerup.gameObject.SetActive(true);
        anim_.gameObject.SetActive(false);
        anim = anim_powerup;
        StartCoroutine(PowerupFreeze(true));
    }

    public void end_powerup()
    {
        anim_powerup.gameObject.SetActive(false);
        anim_.gameObject.SetActive(true);
        anim = anim_;
        StartCoroutine(PowerupFreeze(false));
    }
    public void TriggerInvincibility(float duration)
    {
        if (hitCoroutine != null) StopCoroutine(hitCoroutine);
        StartCoroutine(InvincibilityRoutine(duration));
    }

    private IEnumerator InvincibilityRoutine(float duration)
    {
        isInvincible = true;
        float elapsed = 0f;
        bool visible = true;

        while (elapsed < duration)
        {
            elapsed += 0.1f;
            visible = !visible;
            spriteRenderer.enabled = visible;
            yield return new WaitForSecondsRealtime(0.1f);
        }

        spriteRenderer.enabled = true;
        isInvincible = false;
    }
}