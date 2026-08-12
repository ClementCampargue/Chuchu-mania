using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class SC_player : MonoBehaviour
{
    private Coroutine invincibilityCoroutine;

    [Header("Power_up_stats")]
    public float PowermoveSpeed = 5f;
    public float PowerJump = 5f;
    public SC_juiciness juice;

    [Header("Movement")]
    public float moveSpeed = 5f;
    [HideInInspector] public float base_speed;
    public float jumpForce = 10f;
    public float maxJumpTime = 0.3f;

    [Header("Coyote Time")]
    public float coyoteTime = 0.1f;
    private float coyoteTimeCounter;

    [Header("Jump Input Buffer")]
    public float jumpBufferTime = 0.1f;
   [HideInInspector] public float jumpBufferCounter;

    private float jumpTimeCounter;
    private bool isJumping;
    public bool canJump = true;

    [Header("Climbing")]
    public float climbSpeed = 4f;

    [Range(0f, 1f)]
    public float climbAttachUpThreshold = 0.6f;

    [Range(0f, 1f)]
    public float climbAttachDiagonalLimit = 0.5f;

    public float climbReattachDelay = 0.2f;
    private float climbReattachTimer;

    private bool isClimbing;
    private bool canClimb;
    private SC_grillage grillage;

    public float hitFreezeTime = 0.15f;
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
    public LayerMask bounceLayer;
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
    public Animator anim;

    private Vector2 moveInput;
    public bool isGrounded;
    private bool wasGrounded;
    private bool isFrozen;

    [Header("Inputs")]
    public InputActionReference Jump;
    public InputActionReference Move;

    [Header("Screen Wrap")]
    private Vector2 limit;
    public Transform ghost;

    private Coroutine hitCoroutine;
    private Vector2 knockbackVelocity;

    public SC_icecream_eat_system eat_system;

    public static SC_player instance;

    public BoxCollider2D collider;
    public float base_gravity;
    public GameObject game_over_screen;
    public bool canMove;
    public bool burning;

    private Vector2 added_velocity;
    private Vector2 added_velocity_;

    [Header("Audio")]
    public SC_juiciness jump;
    public SC_juiciness damage_sfx;
    public SC_juiciness bounce_sfx;
    public SC_juiciness damage_lava_sfx;
    public SC_juiciness stun;
    public SC_juiciness transformation;
    public SC_juiciness die;
    public SC_juiciness land;

    public AudioSource grid;
    public AudioSource run;

    private Collider2D hit;

    private sc_health_system health;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        base_speed = moveSpeed;
        instance = this;
    }


    // =========================================================
    // ENABLE / DISABLE INPUTS
    // =========================================================

    private void OnEnable()
    {
        Jump.action.Enable();
        Move.action.Enable();

        Jump.action.performed += OnJumpStarted;
        Jump.action.canceled += OnJumpReleased;
    }


    private void OnDisable()
    {
        Jump.action.performed -= OnJumpStarted;
        Jump.action.canceled -= OnJumpReleased;
    }


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        canTakeDamage = true;

        health = sc_health_system.instance;

        normal.SetActive(true);
        transformed.SetActive(false);

        base_gravity = rb.gravityScale;

        spriteRenderer.material = normalMaterial;
    }


    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        limit = SC_level_master.instance.limits;

        CheckStun();
        HandleLowHealthBlink();


        // =====================================================
        // TIMER DE RÉACCROCHE
        // =====================================================

        if (climbReattachTimer > 0f)
        {
            climbReattachTimer -= Time.deltaTime;

            if (climbReattachTimer < 0f)
            {
                climbReattachTimer = 0f;
            }
        }


        if (isStunned)
        {
            moveInput = Vector2.zero;
            return;
        }


        // =====================================================
        // INPUT
        // =====================================================

        if (!isFrozen &&canMove && !eat_system.isEating &&Time.timeScale != 0)
        {
            Vector2 input =Move.action.ReadValue<Vector2>();


            moveInput = new Vector2(
                Mathf.Abs(input.x) > 0.1f
                    ? Mathf.Sign(input.x)
                    : 0f,

                Mathf.Abs(input.y) > 0.1f
                    ? Mathf.Sign(input.y)
                    : 0f
            );


            // =================================================
            // TENTATIVE D'ACCROCHE
            // =================================================

            if (!isClimbing && canClimb && climbReattachTimer <= 0f)
            {
                TryStartClimbing(input);
            }
        }
        else
        {
            moveInput = Vector2.zero;
        }


        // =====================================================
        // GROUND CHECK
        // =====================================================

        isGrounded =
            Physics2D.OverlapCircle(
                groundCheckBottom.position,
                groundCheckRadius,
                groundLayer
            );


        // =====================================================
        // COYOTE TIME
        // =====================================================

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }


        // =====================================================
        // JUMP INPUT BUFFER
        // =====================================================

        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
        }


        // =====================================================
        // BUFFER + COYOTE JUMP
        // =====================================================

        if (jumpBufferCounter > 0f)
        {
            TryJump();
        }


        // =====================================================
        // ANIMATION / AUDIO CLIMB
        // =====================================================

        if (isClimbing)
        {
            if (moveInput.sqrMagnitude > 0.01f)
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
            // =================================================
            // RUN NORMAL
            // =================================================

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


        // =====================================================
        // LANDING
        // =====================================================

        if (IsAnimationPlaying("jump_idle") &&
            isGrounded &&
            rb.linearVelocity.y == 0)
        {
            anim.ResetTrigger("Jump");
            anim.SetTrigger("Land");

            land.PlayJuice();
        }


        if (!wasGrounded && isGrounded)
        {
            if (rb.linearVelocity.y <= 0.5f)
            {
                anim.ResetTrigger("Jump");
                anim.SetTrigger("Land");

                land.PlayJuice();
            }
            else if (
                transform.localScale.y == -1 &&
                rb.linearVelocity.y >= -0.5f)
            {
                anim.ResetTrigger("Jump");
                anim.SetTrigger("Land");

                land.PlayJuice();
            }
        }


        // =====================================================
        // FACING
        // =====================================================

        float yScale = transform.localScale.y;

        if (moveInput.x > 0)
        {
            transform.localScale =
                new Vector3(1, yScale, 1);
        }
        else if (moveInput.x < 0)
        {
            transform.localScale =
                new Vector3(-1, yScale, 1);
        }


        wasGrounded = isGrounded;


        // =====================================================
        // JUMP
        // =====================================================

        if (isJumping)
        {
            if (jumpTimeCounter > 0)
            {
                float jumpPower =
                    eat_system.isPowerUpActive
                        ? PowerJump
                        : jumpForce;

                float direction =
                    Mathf.Sign(rb.gravityScale);

                rb.linearVelocity =
                    new Vector2(
                        rb.linearVelocity.x,
                        jumpPower * direction
                    );

                jumpTimeCounter -= Time.deltaTime;
            }
            else
            {
                isJumping = false;
            }
        }
    }


    // =========================================================
    // TRY JUMP
    // =========================================================

    private void TryJump()
    {
        if (Time.timeScale == 0)
            return;

        if (!canJump)
            return;

        if (isStunned)
            return;

        if (isFrozen || !canMove)
            return;


        // =====================================================
        // SAUT DEPUIS UNE GRILLE
        // =====================================================

        if (isClimbing)
        {
            StopClimbingJump();

            isJumping = true;

            jumpTimeCounter = maxJumpTime;

            rb.linearVelocity =
                new Vector2(
                    rb.linearVelocity.x,
                    jumpForce
                );

            anim.SetTrigger("Jump");

            jump.PlayJuice();

            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;

            return;
        }


        // =====================================================
        // SAUT NORMAL
        // =====================================================

        if (rb.linearVelocity.y < 0.5f)
        {
            bool canPerformJump =
                isGrounded ||
                coyoteTimeCounter > 0f ||
                eat_system.isPowerUpActive;

            if (canPerformJump)
            {
                isJumping = true;

                jumpTimeCounter = maxJumpTime;

                float jumpPower =
                    eat_system.isPowerUpActive
                        ? PowerJump
                        : jumpForce;

                rb.linearVelocity =
                    new Vector2(
                        rb.linearVelocity.x,
                        jumpPower
                    );

                anim.SetTrigger("Jump");

                jump.PlayJuice();

                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
            }
        }
    }


    // =========================================================
    // JUMP INPUT
    // =========================================================

    private void OnJumpStarted(
        InputAction.CallbackContext context)
    {
        if (Time.timeScale == 0)
            return;

        if (!canJump)
            return;

        if (isStunned)
            return;

        jumpBufferCounter = jumpBufferTime;

        TryJump();
    }


    private void OnJumpReleased(
        InputAction.CallbackContext context)
    {
        isJumping = false;
    }


    // =========================================================
    // ANIMATION CHECK
    // =========================================================

    bool IsAnimationPlaying(string animationName)
    {
        AnimatorStateInfo stateInfo =
            anim.GetCurrentAnimatorStateInfo(0);

        return stateInfo.IsName(animationName);
    }


    // =========================================================
    // STUN
    // =========================================================

    void CheckStun()
    {
        if (isStunned)
            return;

        if (isInvincible)
            return;

        if (eat_system.isPowerUpActive)
            return;

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
        if (eat_system.isPowerUpActive)
            return;

        if (isInvincible)
            return;

        StartCoroutine(StunCoroutine());
    }


    // =========================================================
    // DAMAGE DETECTION
    // =========================================================


    IEnumerator delay_death_enemy()
    {
        yield return new WaitForSecondsRealtime(1f);


        if (hit != null)
        {
            SortingGroup sortingGroup =
                hit.GetComponentInParent<SortingGroup>();

            if (sortingGroup != null)
            {
                sortingGroup.sortingLayerName = "Default";
            }
        }
    }


    // =========================================================
    // STUN PLAYER
    // =========================================================

    public void stun_player()
    {
        if (isStunned ||
            isInvincible ||
            eat_system.isPowerUpActive)
        {
            return;
        }

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


    // =========================================================
    // LOW HEALTH
    // =========================================================

    void HandleLowHealthBlink()
    {
        if (health.current_health == 1)
        {
            if (lowHealthCoroutine == null)
            {
                lowHealthCoroutine =
                    StartCoroutine(LowHealthBlink());
            }
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


        while (health.current_health == 1)
        {
            spriteRenderer.material =
                toggle
                    ? normalMaterial
                    : lowHealthMaterial;

            toggle = !toggle;

            yield return new WaitForSeconds(lowHealthBlinkRate);
        }


        spriteRenderer.material = normalMaterial;

        lowHealthCoroutine = null;
    }


    // =========================================================
    // FIXED UPDATE
    // =========================================================

    void FixedUpdate()
    {
        added_velocity_ =
            Vector2.Lerp(
                added_velocity_,
                added_velocity,
                0.2f
            );

        if (eat_system != null && eat_system.isEating)
        {
            moveInput = Vector2.zero;

            return;
        }

        if (!canMove)
            return;

        if (Time.timeScale == 0)
            return;


        // =====================================================
        // CLIMB
        // =====================================================

        if (isClimbing)
        {
            if (grillage == null)
            {
                StopClimbing();
                return;
            }


            Vector2 climbInput =
                Move.action.ReadValue<Vector2>();


            if (climbInput.magnitude < 0.1f)
            {
                climbInput = Vector2.zero;
            }
            else
            {
                climbInput.Normalize();
            }


            Vector2 climbVelocity =
                climbInput * climbSpeed;


            rb.linearVelocity = climbVelocity;


            Vector2 nextPosition =
                rb.position +
                climbVelocity *
                Time.fixedDeltaTime;


            rb.position =
                grillage.ClampPosition(nextPosition);


            return;
        }


        // =====================================================
        // MOVEMENT NORMAL
        // =====================================================
        if (!isFrozen)
        {


            float horizontalSpeed =
                moveInput.x *
                (
                    eat_system.isPowerUpActive
                        ? PowermoveSpeed
                        : moveSpeed
                )
                + knockbackVelocity.x;
            float verticalSpeed =
                rb.linearVelocity.y;


            if (knockbackVelocity.y != 0)
            {
                verticalSpeed =
                    knockbackVelocity.y;

                knockbackVelocity.y = 0;
            }


            rb.linearVelocity =
                new Vector2(
                    horizontalSpeed +
                    added_velocity_.x,

                    verticalSpeed +
                    added_velocity_.y
                );


            knockbackVelocity.x =
                Mathf.Lerp(
                    knockbackVelocity.x,
                    0,
                    0.15f
                );
        }
    }


    // =========================================================
    // LATE UPDATE
    // =========================================================

    void LateUpdate()
    {
        float x = transform.position.x;


        // =====================================================
        // GHOST SCREEN WRAP
        // =====================================================

        if (x > (limit.y - 0.2f))
        {
            ghost.gameObject.SetActive(true);

            ghost.localScale =
                transform.localScale;


            float distance =
                limit.y - x;


            float ghostX =
                limit.x - distance;


            ghost.position =
                new Vector3(
                    ghostX,
                    transform.position.y,
                    transform.position.z
                );
        }
        else if (x < (limit.x + 0.2f))
        {
            ghost.gameObject.SetActive(true);

            ghost.localScale =
                transform.localScale;


            float distance =
                x - limit.x;


            float ghostX =
                limit.y + distance;


            ghost.position =
                new Vector3(
                    ghostX,
                    transform.position.y,
                    transform.position.z
                );
        }
        else
        {
            ghost.gameObject.SetActive(false);
        }


        // =====================================================
        // SCREEN WRAP
        // =====================================================

        if (x > limit.y)
        {
            transform.position =
                new Vector3(
                    limit.x,
                    transform.position.y,
                    transform.position.z
                );


            if (rb.linearVelocity.x > 0)
            {
                rb.linearVelocity =
                    new Vector2(
                        -Mathf.Abs(
                            rb.linearVelocity.x
                        ),
                        rb.linearVelocity.y
                    );
            }
        }
        else if (x < limit.x)
        {
            transform.position =
                new Vector3(
                    limit.y,
                    transform.position.y,
                    transform.position.z
                );


            if (rb.linearVelocity.x < 0)
            {
                rb.linearVelocity =
                    new Vector2(
                        Mathf.Abs(
                            rb.linearVelocity.x
                        ),
                        rb.linearVelocity.y
                    );
            }
        }
    }


    // =========================================================
    // TAKE DAMAGE
    // =========================================================
    public void TakeDamage(
        int damage,
        Vector2 ejection_power,
        Vector3 sourcePosition)
    {
        // Double sécurité.
        if (!canTakeDamage ||
            isInvincible ||
            burning)
        {
            return;
        }

        if (isFrozen ||
            eat_system.isPowerUpActive)
        {
            return;
        }

        anim.SetBool("Stun", false);
        anim.SetTrigger("Hit");

        if (isClimbing)
        {
            StopClimbingJump();
        }

        // =====================================================
        // BOUNCE
        // Même logique que le HIT, mais sans FREEZE
        // =====================================================

        if (damage == 0)
        {
            bounce_sfx.PlayJuice();

            if (hitCoroutine != null)
            {
                StopCoroutine(hitCoroutine);
            }

            hitCoroutine = StartCoroutine(
                BounceWithKnockback(
                    sourcePosition,
                    ejection_power
                )
            );
        }
        else
        {
            // =================================================
            // DAMAGE NORMAL
            // =================================================

            health.take_damage(damage);

            eat_system.take_damage();

            damage_sfx.PlayJuice();

            if (health.current_health > 0)
            {
                if (hitCoroutine != null)
                {
                    StopCoroutine(hitCoroutine);
                }

                hitCoroutine = StartCoroutine(
                    HitFreezeWithKnockback(
                        sourcePosition,
                        ejection_power
                    )
                );

                StartInvincibility(invincibilityTime);
            }
            else
            {
                Die();
            }
        }

        isStunned = false;
    }

    // =========================================================
    // LAVA HIT
    // =========================================================

    public void LavaHit(
        Vector2 launchVelocity,
        float controlMultiplier,
        float controlTime)
    {
        
        burning = true;


        rb.bodyType =
            RigidbodyType2D.Dynamic;


        rb.linearVelocity =
            launchVelocity;


        StartCoroutine(
            LavaControlLock(
                controlMultiplier,
                controlTime
            )
        );


        if (isInvincible ||
            !canTakeDamage)
        {
            return;
        }

        if (eat_system.isPowerUpActive )
        {
            return;
        }


        if (health.current_health == 0)
            return;



        anim.SetBool(
            "Stun",
            false
        );


        anim.SetTrigger(
            "Hit"
        );


        isStunned = false;


        isFrozen = false;
        isStunned = false;


        eat_system.take_damage();

        health.take_damage(1);


        if (health.current_health > 0)
        {
            if (hitCoroutine != null)
            {
                StopCoroutine(hitCoroutine);
            }


            // IMPORTANT :
            // Même système d'invincibilité que les dégâts normaux.
            StartInvincibility(
                invincibilityTime
            );
        }
        else
        {
            Die();
        }


        damage_lava_sfx.PlayJuice();
    }


    // =========================================================
    // LAVA CONTROL
    // =========================================================
    private IEnumerator BounceWithKnockback(
    Vector3 sourcePosition,
    Vector2 power)
    {
        // Même calcul de direction que le HitFreeze
        Vector2 direction =
            (
                transform.position -
                sourcePosition
            ).normalized;

        knockbackVelocity =
            new Vector2(
                direction.x *
                hitKnockback.x *
                power.x,

                hitKnockback.y *
                transform.localScale.y *
                power.y
            );

        // Même comportement de relâchement du knockback
        yield return new WaitForSeconds(0.1f);

    }
    private IEnumerator LavaControlLock(
        float multiplier,
        float time)
    {
        float originalMove =
            moveSpeed;


        float originalPower =
            PowermoveSpeed;


        moveSpeed *= multiplier;

        PowermoveSpeed *= multiplier;


        yield return new WaitForSeconds(time);


        yield return new WaitUntil(
            () => isGrounded || isClimbing
        );


        burning = false;


        moveSpeed =
            originalMove;


        PowermoveSpeed =
            originalPower;
    }


    // =========================================================
    // INVINCIBILITY
    // =========================================================

    public void TriggerInvincibility(
        float duration)
    {
        StartInvincibility(duration);
    }


    public void StartInvincibility(
        float duration)
    {
        if (invincibilityCoroutine != null)
        {
            StopCoroutine(invincibilityCoroutine);
        }


        invincibilityCoroutine =
            StartCoroutine(
                InvincibilityRoutine(duration)
            );
    }


    private IEnumerator InvincibilityRoutine(
        float duration)
    {
        // =====================================================
        // IMPORTANT :
        // Les deux protections sont activées ensemble.
        // =====================================================

        isInvincible = true;
        canTakeDamage = false;


        float elapsed = 0f;
        bool visible = true;


        while (elapsed < duration)
        {
            visible = !visible;


            spriteRenderer.enabled =
                visible;


            yield return new WaitForSecondsRealtime(0.1f);


            elapsed += 0.1f;
        }


        spriteRenderer.enabled = true;


        isInvincible = false;


        // On ne réactive les dégâts qu'une fois
        // l'invincibilité complètement terminée.
        canTakeDamage = true;


        invincibilityCoroutine = null;
    }


    // =========================================================
    // HIT FREEZE + KNOCKBACK
    // =========================================================

    private IEnumerator HitFreezeWithKnockback(
        Vector3 sourcePosition, Vector2 power)
    {
        isFrozen = true;

        canTakeDamage = false;



        rb.linearVelocity =
            Vector2.zero;


        rb.bodyType =
            RigidbodyType2D.Kinematic;





        yield return new WaitForSecondsRealtime(
            hitFreezeTime
        );


        rb.bodyType =
            RigidbodyType2D.Dynamic;



        isFrozen = false;


        Vector2 direction =
            (
                transform.position -
                sourcePosition
            ).normalized;


        knockbackVelocity =
            new Vector2(
                direction.x *
                hitKnockback.x * power.x,

                hitKnockback.y *
                transform.localScale.y * power.y
            );


        yield return new WaitForSeconds(0.1f);


        // =====================================================
        // IMPORTANT :
        // Ne jamais réactiver les dégâts pendant
        // une invincibilité encore active.
        // =====================================================

        if (!isInvincible)
        {
            canTakeDamage = true;
        }
    }


    // =========================================================
    // POWERUP FREEZE
    // =========================================================

    private IEnumerator PowerupFreeze(
        bool isActivating)
    {
        isFrozen = true;


        rb.linearVelocity =
            Vector2.zero;


        rb.bodyType =
            RigidbodyType2D.Kinematic;


        string trigger =
            isActivating
                ? transformAnimTrigger
                : detransformAnimTrigger;


        anim.SetTrigger(trigger);


        float originalTimeScale =
            Time.timeScale;


        Time.timeScale = 0f;


        yield return new WaitForSecondsRealtime(
            transformFreezeTime
        );


        Time.timeScale =
            originalTimeScale;


        rb.bodyType =
            RigidbodyType2D.Dynamic;


        isFrozen = false;
    }


    // =========================================================
    // DIE
    // =========================================================

    public void Die()
    {
        canMove = false;


        if (hitCoroutine != null)
        {
            StopCoroutine(hitCoroutine);
        }

        rb.linearVelocity =
        Vector2.zero;

        GetComponent<SortingGroup>()
            .sortingLayerName = "UI";
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        rb.gravityScale = 0;
        isFrozen = true;
        rb.linearVelocity =
        Vector2.zero;


        anim.SetBool("Die", true);

        anim.SetBool("Eat", false);


        eat_system.eating_sfx.Stop();


        game_over_screen.SetActive(true);


        SC_music_manager.instance
            .stop_music();


        collider.enabled = false;


        die.PlayJuice();


        knockbackVelocity =
            Vector2.zero;


        Time.timeScale = 0;
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    void OnDrawGizmosSelected()
    {
        if (damageCheck != null)
        {
            Gizmos.color = Color.red;


            Gizmos.DrawWireSphere(
                damageCheck.position,
                damageRadius
            );
        }
    }


    // =========================================================
    // POWERUP
    // =========================================================

    public void powerup()
    {
        anim.ResetTrigger("Punch");
        anim.SetBool("Eat",false);

        anim.SetTrigger("Transform");


        transformation.PlayJuice();


        normal.SetActive(false);

        transformed.SetActive(true);


        StartCoroutine(
            PowerupFreeze(true)
        );
    }


    public void end_powerup()
    {
        normal.SetActive(true);

        transformed.SetActive(false);


        StartCoroutine(
            PowerupFreeze(false)
        );
    }


    // =========================================================
    // CLIMB TRIGGERS
    // =========================================================

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        if (other.CompareTag("Climb"))
        {
            canClimb = true;


            grillage =
                other.GetComponent<SC_grillage>();
        }
    }


    private void OnTriggerStay2D(
        Collider2D other)
    {
        if (other.CompareTag("Climb"))
        {
            canClimb = true;


            if (grillage == null)
            {
                grillage =
                    other.GetComponent<SC_grillage>();
            }
        }
    }


    private void OnTriggerExit2D(
        Collider2D other)
    {
        if (other.CompareTag("Climb"))
        {
            SC_grillage exitedGrillage =
                other.GetComponent<SC_grillage>();


            if (grillage == exitedGrillage)
            {
                StopClimbing();


                grillage = null;


                canClimb = false;
            }
        }
    }


    // =========================================================
    // TRY START CLIMBING
    // =========================================================

    private void TryStartClimbing(
        Vector2 input)
    {
        if (!canClimb)
            return;

        if (isClimbing)
            return;

        if (grillage == null)
            return;

        if (climbReattachTimer > 0f)
            return;


        if (input.y <
            climbAttachUpThreshold)
        {
            return;
        }


        if (Mathf.Abs(input.x) >
            input.y *
            climbAttachDiagonalLimit)
        {
            return;
        }


        StartClimbing();
    }


    // =========================================================
    // START CLIMBING
    // =========================================================

    void StartClimbing()
    {
        if (!canClimb)
            return;

        if (grillage == null)
            return;

        if (climbReattachTimer > 0f)
            return;


        isClimbing = true;


        rb.gravityScale = 0;


        rb.linearVelocity =
            Vector2.zero;


        anim.SetBool(
            "Climb",
            true
        );
    }


    // =========================================================
    // STOP CLIMBING
    // =========================================================

    void StopClimbing()
    {
        if (!isClimbing)
            return;


        isClimbing = false;


        rb.gravityScale =
            base_gravity;


        rb.linearVelocity =
            Vector2.zero;


        grid.Stop();


        anim.SetBool(
            "Climb",
            false
        );
    }


    // =========================================================
    // STOP CLIMBING POUR SAUT
    // =========================================================

    void StopClimbingJump()
    {
        if (!isClimbing)
            return;


        isClimbing = false;


        climbReattachTimer =
            climbReattachDelay;


        rb.gravityScale =
            base_gravity;


        rb.linearVelocity =
            Vector2.zero;


        grid.Stop();


        anim.SetBool(
            "Climb",
            false
        );
    }


    // =========================================================
    // FACE TARGET
    // =========================================================

    public void FaceTarget(
        Transform target)
    {
        if (target == null)
            return;


        float direction =
            target.position.x -
            transform.position.x;


        if (direction > 0)
        {
            transform.localScale =
                new Vector3(
                    Mathf.Abs(
                        transform.localScale.x
                    ),
                    transform.localScale.y,
                    transform.localScale.z
                );
        }
        else if (direction < 0)
        {
            transform.localScale =
                new Vector3(
                    -Mathf.Abs(
                        transform.localScale.x
                    ),
                    transform.localScale.y,
                    transform.localScale.z
                );
        }
    }


    // =========================================================
    // GROUND VELOCITY
    // =========================================================

    public void SetGroundVelocity(
        Vector2 vel)
    {
        added_velocity = vel;
    }


    // =========================================================
    // REVIVE
    // =========================================================

    public void Revive()
    {
        normal.SetActive(true);

        transformed.SetActive(false);


        StopAllCoroutines();


        isFrozen = false;
        isStunned = false;
        isInvincible = false;
        canTakeDamage = true;


        isClimbing = false;
        canClimb = false;
        grillage = null;


        climbReattachTimer = 0f;


        rb.gravityScale =
            base_gravity;




        GetComponent<SortingGroup>()
            .sortingLayerName = "Default";


        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;
        isJumping = false;


        if (health != null)
        {
            health.revive();
        }


        collider.enabled = true;


        spriteRenderer.enabled = true;


        rb.bodyType =
            RigidbodyType2D.Dynamic;


        rb.linearVelocity =
            Vector2.zero;


        rb.constraints =
            RigidbodyConstraints2D.FreezeRotation;


        rb.angularVelocity = 0;


        eat_system.ResetSystem();


        anim.SetBool(
            "Die",
            false
        );
        anim.SetBool(
         "Climb",
         false
     ); anim.SetTrigger("Start");
        transform.localScale = Vector3.one;
    }
}
