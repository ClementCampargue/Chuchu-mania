using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class SC_player : MonoBehaviour
{
    [Header("Power_up_stats")]
    public float PowermoveSpeed = 5f;
    public float PowerJump = 5f;
    public SC_juiciness juice;
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public float maxJumpTime = 0.3f;
    private float jumpTimeCounter;
    private bool isJumping;

    [Header("Climbing")]
    public float climbSpeed = 4f;

    private bool isClimbing;
    private bool canClimb;
    private SC_grillage grillage;
    [Header("Health")]
    public int maxHealth = 3;
    private int currentHealth;
    public float hitFreezeTime = 0.15f;
    public List<GameObject> hearts;
    public Vector2 hitKnockback = new Vector2(5f, 3f);
    [Header("Low Health Warning")]
    public Material normalMaterial;
    public Material lowHealthMaterial;
    public float lowHealthBlinkRate = 0.2f;

    private Coroutine lowHealthCoroutine;
    [Header("Ground Check")]
    public Transform groundCheckBottom;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Damage Detection")]
    public Transform damageCheck;
    public float damageRadius = 0.5f;
    public LayerMask damageLayer;
    private bool canTakeDamage = true;
    [Header("Stun")]
    public LayerMask stunLayer;
    public float stunDuration = 2f;

    private bool isStunned = false;
    [Header("Invincibility")]
    public float invincibilityTime = 1f;
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer spriteRendererPower;
    public bool isInvincible = false;

    [Header("Transformation")]
    public GameObject normal;
    public GameObject transformed;
    public float transformFreezeTime = 0.5f;
    public string transformAnimTrigger = "Transform";
    public string detransformAnimTrigger = "DeTransform";

    public Rigidbody2D rb;
    public Animator anim_;
    private Animator anim;

    private Vector2 moveInput;
    public bool isGrounded;
    private bool wasGrounded;
    private bool isFrozen;

    [Header("Inputs")]
    public InputActionReference Jump;
    public InputActionReference Move;

    [Header("Screen Wrap")]
    private float limit = -10f;
    public Transform ghost;

    private float levelWidth;
    private Coroutine hitCoroutine;
    private Vector2 knockbackVelocity;
    public ParticleSystem ps_damage;

    private SC_icecream_eat_system eat_system;
    public static SC_player instance;
    public BoxCollider2D collider;
    public float base_gravity;
    public GameObject game_over_screen;
    public bool canMove;
    public bool burning;
    private bool was_climbing; 
    private Vector2 added_velocity;
    private Vector2 added_velocity_;

    [Header("Audio")]
    public SC_juiciness jump;
    public SC_juiciness damage_sfx;
    public SC_juiciness damage_lava_sfx;
    public SC_juiciness stun;
    public SC_juiciness transformation;
    public SC_juiciness die;
    public SC_juiciness land;
    public AudioSource grid;
    public AudioSource run;
    private Collider2D hit;
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
        normal.SetActive(true);
        transformed.SetActive(false);
        limit = SC_game_master.instance.limits;
        base_gravity = rb.gravityScale;
        spriteRenderer.material = normalMaterial;
        eat_system = SC_icecream_eat_system.instance;
        currentHealth = maxHealth;
        anim = anim_;
    }

    void Update()
    {
        if (!SC_level_intro.gameStarted) return;
        CheckStun();
        CheckDamage();
        HandleLowHealthBlink();
        if (isStunned)
        {
            moveInput = Vector2.zero;
            return;
        }
  
        if (canClimb)
        {
            float verticalInput = moveInput.y;

            if (Mathf.Abs(verticalInput) > 0.1f)
            {
                StartClimbing(verticalInput);
            }
            else if (isClimbing)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
        if (!isFrozen  && canMove)
            moveInput = Move.action.ReadValue<Vector2>();
        else
            moveInput = Vector2.zero;



        isGrounded = Physics2D.OverlapCircle(
            groundCheckBottom.position,
            groundCheckRadius,
            groundLayer
        );
        if (isClimbing)
        {
            if (Mathf.Abs(moveInput.y) > 0.1f || Mathf.Abs(moveInput.x) > 0.1f)
            {
                if (!grid.isPlaying)
                {
                    grid.Play();
                }

                anim.SetBool("Run", true);
            }
            else
            {
                grid.Stop();
                anim.SetBool("Run", false);
            }

        }
        else
        {


            if (Mathf.Abs(moveInput.x) > 0.1f)
            {
                if (isGrounded)
                {
                    if (!run.isPlaying)
                    {
                        run.Play();
                    }
                 
                }
                else
                {
                    run.Stop();

                }
                anim.SetBool("Run", true);
            }
            else
            {
                run.Stop();

                anim.SetBool("Run", false);
            }
        }

        if (!wasGrounded && isGrounded )
        {
            anim.ResetTrigger("Jump");
            anim.SetTrigger("Land");
            land.PlayJuice();
        }
        float yScale = transform.localScale.y;

        if (moveInput.x > 0)
            transform.localScale = new Vector3(1, yScale, 1);
        else if (moveInput.x < 0)
            transform.localScale = new Vector3(-1, yScale, 1);

        wasGrounded = isGrounded;

        if (isJumping)
        {
            if (jumpTimeCounter > 0)
            {
                float jumpPower = eat_system.isPowerUpActive ? PowerJump : jumpForce;
                float direction = Mathf.Sign(rb.gravityScale);

                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    jumpForce * direction
                );
                jumpTimeCounter -= Time.deltaTime;
            }
            else
            {
                isJumping = false;
            }
        }

    }
    void CheckStun()
    {
        if (isStunned)
            return;
        if (isInvincible) return;

        Collider2D hit =
            Physics2D.OverlapCircle(
                damageCheck.position,
                damageRadius,
                stunLayer
            );

        if (hit != null)
        {
            StartCoroutine(StunCoroutine());
        }
    }

    public void Stun()
    { 
        if (isInvincible) return;
        StartCoroutine(StunCoroutine());
    }
    void CheckDamage()
    {
        if (!canTakeDamage || currentHealth == 0) return;

        hit = Physics2D.OverlapCircle(damageCheck.position, damageRadius, damageLayer);
        if (hit != null)
        {
            TakeDamage(1, hit.transform.position);
            if(currentHealth == 0)
            {
                hit.GetComponentInParent<SortingGroup>().sortingLayerName = "UI";
                StartCoroutine(delay_death_enemy());
            }
        }
    }
    IEnumerator delay_death_enemy()
    {
        yield return new WaitForSecondsRealtime(1.5f);


        hit.GetComponentInParent<SortingGroup>().sortingLayerName = "Default";


    }

    public void stun_player()
    {
        if (isStunned)
            return;
        StartCoroutine(StunCoroutine());

    }
    IEnumerator StunCoroutine()
    {
        juice.PlayJuice();
        isStunned = true;
        anim.SetBool("Stun", true);
        rb.linearVelocity = Vector2.zero;


        yield return new WaitForSeconds(stunDuration);
        anim.SetBool("Stun", false);


        isStunned = false;
    }
    void HandleLowHealthBlink()
    {
        if (currentHealth == 1)
        {
            if (lowHealthCoroutine == null)
                lowHealthCoroutine = StartCoroutine(LowHealthBlink());
        }
        else
        {
            if (lowHealthCoroutine != null)
            {
                StopCoroutine(lowHealthCoroutine);
                lowHealthCoroutine = null;
            }

            spriteRenderer.material = normalMaterial;
        }
    }
    IEnumerator LowHealthBlink()
    {
        bool toggle = false;

        while (currentHealth == 1)
        {
            spriteRenderer.material = toggle ? normalMaterial : lowHealthMaterial;
            toggle = !toggle;

            yield return new WaitForSeconds(lowHealthBlinkRate);
        }

        spriteRenderer.material = normalMaterial;
        lowHealthCoroutine = null;

    }
    void FixedUpdate()
    {
        added_velocity_ = Vector2.Lerp(added_velocity_, added_velocity, 0.2f);
        if (isClimbing && grillage != null)
        {
            Vector2 clamped = grillage.ClampPosition(rb.position);
            rb.position = clamped;
        }
        if (!SC_level_intro.gameStarted) return;
        if (!isFrozen)
        {
            float horizontalSpeed = moveInput.x * (eat_system.isPowerUpActive ? PowermoveSpeed : moveSpeed) + knockbackVelocity.x;


            float verticalSpeed = rb.linearVelocity.y;
            if (knockbackVelocity.y != 0)
            {
                verticalSpeed = knockbackVelocity.y;
                knockbackVelocity.y = 0;
            }

            rb.linearVelocity = new Vector2(
                horizontalSpeed + added_velocity_.x,
                verticalSpeed + added_velocity_.y
            );
            knockbackVelocity.x = Mathf.Lerp(knockbackVelocity.x, 0, 0.15f);
        }
    }
    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        if (isClimbing)
        {
            StopClimbingJump();

            isJumping = true;
            jumpTimeCounter = maxJumpTime;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            anim.SetTrigger("Jump");
            jump.PlayJuice();
            return;
        }
        if (!isFrozen && canMove && rb.linearVelocity.y <0.5f)
        {
            if (isGrounded || eat_system.isPowerUpActive)
            {
                isJumping = true;
                jumpTimeCounter = maxJumpTime;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                anim.SetTrigger("Jump");
                jump.PlayJuice();
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

        if (x > (limit - levelWidth / 2))
        {
            ghost.gameObject.SetActive(true);
            ghost.position = new Vector3(x - levelWidth, transform.position.y, transform.position.z);
        }
        else if (x < (-limit + levelWidth / 2))
        {
            ghost.gameObject.SetActive(true);
            ghost.position = new Vector3(x + levelWidth, transform.position.y, transform.position.z);
        }
        else
        {
            ghost.gameObject.SetActive(false);
        }

        if (x > limit)
            transform.position = new Vector3(-limit, transform.position.y, transform.position.z);
        else if (x < -limit)
            transform.position = new Vector3(limit, transform.position.y, transform.position.z);
    }


    public void TakeDamage(int damage, Vector3 sourcePosition)
    {
        if (!canTakeDamage) return;

        if (isFrozen || isInvincible || eat_system.isPowerUpActive) return;
        anim.SetBool("Stun", false);
        if (isClimbing)
        {
            StopClimbingJump();
        }
        damage_sfx.PlayJuice();

        hearts[currentHealth-1].SetActive(false);
        isStunned = false;
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
    public void LavaHit(Vector2 launchVelocity, float controlMultiplier, float controlTime)
    {
        burning = true;

        StopAllCoroutines();
        anim.SetBool("Stun", false);
        hearts[currentHealth - 1].SetActive(false);
        isStunned = false;
        ps_damage.Play();
        currentHealth -= 1;
        eat_system.take_damage();

        if (currentHealth > 0)
        {
            if (hitCoroutine != null) StopCoroutine(hitCoroutine);
            StartCoroutine(InvincibilityCoroutine());
        }
        else
        {
            Die();
        }
        isFrozen = false;
        isStunned = false;
        canTakeDamage = true;
        damage_lava_sfx.PlayJuice();

        rb.bodyType = RigidbodyType2D.Dynamic;

        rb.linearVelocity = launchVelocity;

        StartCoroutine(LavaControlLock(controlMultiplier, controlTime));
    }
    private IEnumerator LavaControlLock(float multiplier, float time)
    {
        float originalMove = moveSpeed;
        float originalPower = PowermoveSpeed;

        moveSpeed *= multiplier;
        PowermoveSpeed *= multiplier;

        yield return new WaitForSeconds(time);

        yield return new WaitUntil(() => isGrounded);
        burning = false;

        moveSpeed = originalMove;
        PowermoveSpeed = originalPower;
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
        GetComponent<SortingGroup>().sortingLayerName = "UI";
        isFrozen = true;
        anim.SetBool("Die",true);
        game_over_screen.SetActive(true);
        SC_music_manager.instance.stop_music();
        collider.enabled = false;
        die.PlayJuice();
        knockbackVelocity = Vector2.zero;

        Time.timeScale = 0;

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
        anim.SetTrigger("Transform");
        transformation.PlayJuice();
        normal.SetActive(false);
        transformed.SetActive(true);
        StartCoroutine(PowerupFreeze(true));
    }

    public void end_powerup()
    {
        normal.SetActive(true);
        transformed.SetActive(false);
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
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Climb"))
        {
            canClimb = true;
            grillage = other.GetComponent<SC_grillage>();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Climb"))
        {
            if (was_climbing)
            {
                StartClimbing(0);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Climb"))
        {
            grillage = null;
            StopClimbing();
            canClimb = false;
        }
    }
    void StartClimbing(float verticalInput)
    {
        isClimbing = true;

        rb.gravityScale = 0;

        Vector2 velocity = new Vector2(0, verticalInput * climbSpeed);

        if (grillage != null)
        {
            Vector2 clamped = grillage.ClampPosition(rb.position + velocity * Time.fixedDeltaTime);
            rb.position = clamped;
            rb.linearVelocity = velocity;
        }
        else
        {
            rb.linearVelocity = velocity;
        }

        anim.SetBool("Climb", true);
    }
    void StopClimbing()
    {
        canClimb = false;
        was_climbing = true;
        grillage = null;
        grid.Stop();

        isClimbing = false;
        Invoke("Delay_climb",0.02f);
        rb.gravityScale = base_gravity;
        anim.SetBool("Climb", false);
    }

    void StopClimbingJump()
    {
        canClimb = false;

        isClimbing = false;
        rb.gravityScale = base_gravity;
        anim.SetBool("Climb", false);
    }

    void Delay_climb()
    {
        was_climbing = false;
    }
    public void SetGroundVelocity(Vector2 vel)
    {
        added_velocity = vel;
    }
    public void Revive()
    {
        StopAllCoroutines();

        // Réinitialisation des états
        isFrozen = false;
        isStunned = false;
        isInvincible = false;
        burning = false;
        canTakeDamage = true;

        // Restaurer la vie
        currentHealth = maxHealth;

        // Réactiver les cœurs
        foreach (GameObject heart in hearts)
        {
            heart.SetActive(true);
        }

        // Réactiver les composants
        collider.enabled = true;
        spriteRenderer.enabled = true;

        // Réinitialiser la physique
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = base_gravity;
        rb.linearVelocity = Vector2.zero;
        knockbackVelocity = Vector2.zero;

        // Réinitialiser les animations
        anim.SetBool("Die", false);
        anim.SetBool("Stun", false);
        anim.SetBool("Climb", false);

        // Déplacer au point de respawn
        if (transform.position != null)
            transform.position = transform.position;

        // Fermer l'écran Game Over
        game_over_screen.SetActive(false);

        // Remettre le temps normal
        Time.timeScale = 1f;

        // Petite invincibilité après respawn
        StartCoroutine(InvincibilityRoutine(2f));
    }
}