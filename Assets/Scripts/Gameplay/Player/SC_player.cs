using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class SC_player : MonoBehaviour
{
    private Coroutine invincibilityCoroutine;
    private Coroutine lowHealthCoroutine;
    private Coroutine hitCoroutine;
    private Coroutine dropThroughCoroutine;
    public bool climb_delay = true;
    [Header("Power_up_stats")]
    public float PowermoveSpeed = 5f;
    public float BasePowermoveSpeed = 5f;
    public float PowerJump = 5f;
    public SC_juiciness juice;
    public BoxCollider2D collision;
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

    [Header("Drop Through Platform")]
    [Tooltip("Layer des plateformes que le joueur peut traverser vers le bas.")]
    public LayerMask platformLayer;

    [Tooltip("Durée pendant laquelle les collisions avec les plateformes sont ignorées.")]
    public float dropThroughTime = 0.25f;

    [Tooltip("Force verticale appliquée lors du passage au travers.")]
    public float dropThroughVelocity = 2f;

    private bool isDroppingThrough;

    [Header("Hit")]
    public float hitFreezeTime = 0.15f;
    public Vector2 hitKnockback = new Vector2(5f, 3f);

    [Header("Low Health Warning")]
    public Material normalMaterial;
    public Material lowHealthMaterial;
    public float lowHealthBlinkRate = 0.2f;

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
    public bool isStunned;

    [Header("Invincibility")]
    public float invincibilityTime = 1f;
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer spriteRendererPower;

    public bool isInvincible;

    [Header("Transformation")]
    public GameObject normal;
    public GameObject transformed;
    public float transformFreezeTime = 0.5f;

    public string transformAnimTrigger = "Transform";
    public string detransformAnimTrigger = "DeTransform";

    [Header("Components")]
    public Rigidbody2D rb;
    public Animator anim;
    public BoxCollider2D collider;

    [Header("Inputs")]
    public InputActionReference Jump;
    public InputActionReference Move;

    [Header("Screen Wrap")]
    private Vector2 limit;
    public Transform ghost;

    [Header("Systems")]
    public SC_icecream_eat_system eat_system;
    public static SC_player instance;

    public float base_gravity;

    public GameObject game_over_screen;

    public bool canMove = true;
    public bool burning;

    [Header("External Velocity")]
    public Vector2 externalVelocity;

    [Header("Knockback")]
    public Vector2 knockbackVelocity;

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

    public Vector2 moveInput;
    public bool isGrounded;

    public bool wasGrounded;
    public bool isFrozen;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        instance = this;

        base_speed = moveSpeed;
        BasePowermoveSpeed = PowermoveSpeed;
    }


    // =========================================================
    // ENABLE
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

    private void Start()
    {
        canTakeDamage = true;
        climb_delay = true;
        health = sc_health_system.instance;

        normal.SetActive(true);
        transformed.SetActive(false);

        base_gravity = rb.gravityScale;

        spriteRenderer.material = normalMaterial;
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        limit = SC_level_master.instance.limits;

        CheckStun();
        HandleLowHealthBlink();

        // -----------------------------------------------------
        // CLIMB REATTACH TIMER
        // -----------------------------------------------------

        if (climbReattachTimer > 0f)
        {
            climbReattachTimer -= Time.deltaTime;

            if (climbReattachTimer < 0f)
                climbReattachTimer = 0f;
        }


        // -----------------------------------------------------
        // STUN
        // -----------------------------------------------------

        if (isStunned)
        {
            moveInput = Vector2.zero;
            return;
        }


        // -----------------------------------------------------
        // INPUT
        // -----------------------------------------------------

        if (!isFrozen &&
            canMove &&
            !eat_system.isEating &&
            Time.timeScale != 0)
        {
            Vector2 input = Move.action.ReadValue<Vector2>();

            moveInput = new Vector2(
                Mathf.Abs(input.x) > 0.2f
                    ? Mathf.Sign(input.x)
                    : 0f,

                Mathf.Abs(input.y) > 0.2f
                    ? Mathf.Sign(input.y)
                    : 0f
            );

            // -------------------------------------------------
            // DROP THROUGH PLATFORM
            // -------------------------------------------------

            if (!isClimbing &&
                !isDroppingThrough &&
                isGrounded &&
                input.y < -0.5f &&
                canClimb &&
                grillage != null)
            {
                StartDropThrough();
            }

            // -------------------------------------------------
            // CLIMB NORMAL
            // -------------------------------------------------

            if (!isClimbing &&
                !isDroppingThrough &&
                canClimb &&
                climbReattachTimer <= 0f)
            {
                TryStartClimbing(input);
            }
        }
        else
        {
            moveInput = Vector2.zero;
        }


        // -----------------------------------------------------
        // GROUND CHECK
        // -----------------------------------------------------

        isGrounded = Physics2D.OverlapCircle(
            groundCheckBottom.position,
            groundCheckRadius,
            groundLayer
        );


        // -----------------------------------------------------
        // COYOTE
        // -----------------------------------------------------

        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;


        // -----------------------------------------------------
        // JUMP BUFFER
        // -----------------------------------------------------

        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0f)
            TryJump();


        // -----------------------------------------------------
        // ANIMATION
        // -----------------------------------------------------

        if (isClimbing)
        {
            if (moveInput.sqrMagnitude > 0.01f)
            {
                if (!grid.isPlaying)
                    grid.Play();

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
                        run.Play();
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


        // -----------------------------------------------------
        // LANDING
        // -----------------------------------------------------

        if (IsAnimationPlaying("jump_idle") &&
            isGrounded &&
            Mathf.Abs(rb.linearVelocity.y) < 0.01f &&
            rb.gravityScale > 0)
        {
            anim.ResetTrigger("Jump");
            anim.SetTrigger("Land");

            moveSpeed = base_speed;

            land.PlayJuice();
        }


        if (IsAnimationPlaying("jump_idle") &&
            isGrounded &&
            Mathf.Abs(rb.linearVelocity.y) > -0.1f &&
            rb.gravityScale < 0)
        {
            anim.ResetTrigger("Jump");
            anim.SetTrigger("Land");

            moveSpeed = base_speed;

            land.PlayJuice();
        }


        if (IsAnimationPlaying("climb_move") &&
            isGrounded &&
            Mathf.Abs(rb.linearVelocity.y) < 0.01f &&
            rb.gravityScale > 0)
        {
            anim.ResetTrigger("Jump");
            anim.SetTrigger("Land");

            moveSpeed = base_speed;

            land.PlayJuice();
        }


        if (IsAnimationPlaying("climb_move") &&
            isGrounded &&
            Mathf.Abs(rb.linearVelocity.y) > -0.1f &&
            rb.gravityScale < 0)
        {
            anim.ResetTrigger("Jump");
            anim.SetTrigger("Land");

            moveSpeed = base_speed;

            land.PlayJuice();
        }


        if (IsAnimationPlaying("hit") &&
            wasGrounded &&
            isGrounded &&
            Mathf.Abs(rb.linearVelocity.y) < 0.1f &&
            rb.gravityScale > 0)
        {
            anim.ResetTrigger("Jump");
            anim.SetTrigger("Land");

            moveSpeed = base_speed;

            land.PlayJuice();
        }


        if (IsAnimationPlaying("hit") &&
            !wasGrounded &&
            isGrounded &&
            Mathf.Abs(rb.linearVelocity.y) > -0.01f &&
            rb.gravityScale < 0)
        {
            anim.ResetTrigger("Jump");
            anim.SetTrigger("Land");

            moveSpeed = base_speed;

            land.PlayJuice();
        }


        // -----------------------------------------------------
        // FACING
        // -----------------------------------------------------

        float yScale = transform.localScale.y;

        if (moveInput.x > 0)
        {
            transform.localScale = new Vector3(
                1,
                yScale,
                1
            );
        }
        else if (moveInput.x < 0)
        {
            transform.localScale = new Vector3(
                -1,
                yScale,
                1
            );
        }


        wasGrounded = isGrounded;


        // -----------------------------------------------------
        // JUMP
        // -----------------------------------------------------

        if (isJumping)
        {
            if (jumpTimeCounter > 0f)
            {
                float jumpPower =
                    eat_system.isPowerUpActive
                        ? PowerJump
                        : jumpForce;

                float direction =
                    Mathf.Sign(rb.gravityScale);

                rb.linearVelocity = new Vector2(
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
    // DROP THROUGH PLATFORM
    // =========================================================

    private void StartDropThrough()
    {
        if (dropThroughCoroutine != null)
            StopCoroutine(dropThroughCoroutine);

        dropThroughCoroutine =
            StartCoroutine(DropThroughPlatforms());
    }


    private IEnumerator DropThroughPlatforms()
    {
        if (isDroppingThrough)
            yield break;

        isDroppingThrough = true;

        // -----------------------------------------------------
        // SAUVEGARDE DES COLLISIONS
        // -----------------------------------------------------

        Collider2D[] playerColliders =
            GetComponentsInChildren<Collider2D>();

        Collider2D[] platforms =
            Physics2D.OverlapCircleAll(
                transform.position,
                1f,
                platformLayer
            );

        // -----------------------------------------------------
        // IGNORE COLLISIONS
        // -----------------------------------------------------

        foreach (Collider2D playerCollider in playerColliders)
        {
            foreach (Collider2D platform in platforms)
            {
                if (playerCollider != null &&
                    platform != null)
                {
                    Physics2D.IgnoreCollision(
                        playerCollider,
                        platform,
                        true
                    );
                }
            }
        }

        // -----------------------------------------------------
        // FORCE VERS LE BAS
        // -----------------------------------------------------

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            -dropThroughVelocity
        );

        // -----------------------------------------------------
        // ATTEND QUE LE JOUEUR TRAVERSE
        // -----------------------------------------------------

        yield return new WaitForSeconds(
            dropThroughTime
        );

        // -----------------------------------------------------
        // RÉACTIVE LES COLLISIONS
        // -----------------------------------------------------

        foreach (Collider2D playerCollider in playerColliders)
        {
            foreach (Collider2D platform in platforms)
            {
                if (playerCollider != null &&
                    platform != null)
                {
                    Physics2D.IgnoreCollision(
                        playerCollider,
                        platform,
                        false
                    );
                }
            }
        }

        isDroppingThrough = false;
        dropThroughCoroutine = null;
    }


    // =========================================================
    // FIXED UPDATE
    // =========================================================

    private void FixedUpdate()
    {
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
                climbInput = Vector2.zero;
            else
                climbInput.Normalize();


            Vector2 climbVelocity;

            if (grillage.isLadder)
            {
                climbVelocity = new Vector2(
                    0f,
                    climbInput.y * climbSpeed
                );
            }
            else
            {
                climbVelocity =
                    climbInput * climbSpeed;
            }


            Vector2 nextPosition =
                rb.position +
                climbVelocity * Time.fixedDeltaTime;


            // -------------------------------------------------
            // LIMITES
            // -------------------------------------------------

            nextPosition =
                grillage.ClampPosition(nextPosition);

            // -------------------------------------------------
            // SORTIE AUTOMATIQUE DE L'ÉCHELLE
            // -------------------------------------------------

            if (grillage.isLadder)
            {
                bool reachedBottom =
                    nextPosition.y <= grillage.minY + 0.5f &&
                    climbInput.y < 0f;

                bool reachedTop =
                    nextPosition.y >= grillage.maxY - 0.5f &&
                    climbInput.y > 0f;

                if (reachedBottom || reachedTop)
                {
                    nextPosition.y =
                        reachedBottom
                            ? grillage.minY
                            : grillage.maxY;

                    nextPosition.x = rb.position.x;

                    rb.position = nextPosition;

                    StopClimbing();

                    return;
                }
            }            // -------------------------------------------------
            // DÉPLACEMENT
            // -------------------------------------------------

            rb.linearVelocity =
                climbVelocity;

            rb.position =
                nextPosition;

            return;
        }


        // =====================================================
        // PHYSICS
        // =====================================================

        float playerHorizontalVelocity = 0f;

        if (!isFrozen &&
            canMove &&
            !eat_system.isEating)
        {
            playerHorizontalVelocity =
                moveInput.x *
                (
                    eat_system.isPowerUpActive
                        ? PowermoveSpeed
                        : moveSpeed
                );
        }


        // =====================================================
        // KNOCKBACK
        // =====================================================

        float knockbackX =
            knockbackVelocity.x;

        float verticalVelocity =
            rb.linearVelocity.y;

        if (knockbackVelocity.y != 0f)
        {
            verticalVelocity =
                knockbackVelocity.y;

            knockbackVelocity.y = 0f;
        }


        // =====================================================
        // VELOCITY FINALE
        // =====================================================

        float finalX =
            playerHorizontalVelocity +
            externalVelocity.x +
            knockbackX;

        float finalY =
            verticalVelocity +
            externalVelocity.y;

        rb.linearVelocity =
            new Vector2(
                finalX,
                finalY
            );


        // =====================================================
        // FADE KNOCKBACK
        // =====================================================

        knockbackVelocity.x =
            Mathf.Lerp(
                knockbackVelocity.x,
                0f,
                0.15f
            );
    }


    // =========================================================
    // EXTERNAL VELOCITY
    // =========================================================

    public void SetGroundVelocity(Vector2 velocity)
    {
        externalVelocity = velocity;
    }


    public void ClearGroundVelocity()
    {
        externalVelocity = Vector2.zero;
    }


    // =========================================================
    // JUMP
    // =========================================================

    public void TryJump()
    {
        if (Time.timeScale == 0)
            return;

        if (!canJump)
            return;

        if (isStunned)
            return;

        if (isFrozen)
            return;

        if (!canMove)
            return;

        if (eat_system.isEating)
            return;


        // -----------------------------------------------------
        // SAUT DEPUIS LA GRILLE
        // -----------------------------------------------------

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


        // -----------------------------------------------------
        // SAUT NORMAL
        // -----------------------------------------------------

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


    private void OnJumpStarted(
        InputAction.CallbackContext context)
    {
        if (Time.timeScale == 0)
            return;

        if (!canJump)
            return;

        if (isStunned)
            return;

        if (eat_system.isEating)
            return;

        jumpBufferCounter =
            jumpBufferTime;

        TryJump();
    }


    private void OnJumpReleased(
        InputAction.CallbackContext context)
    {
        isJumping = false;
    }


    // =========================================================
    // ANIMATION
    // =========================================================

    public bool IsAnimationPlaying(
        string animationName)
    {
        AnimatorStateInfo stateInfo =
            anim.GetCurrentAnimatorStateInfo(0);

        return stateInfo.IsName(
            animationName
        );
    }


    // =========================================================
    // STUN
    // =========================================================

    private void CheckStun()
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
            StartCoroutine(
                StunCoroutine()
            );
    }


    public void Stun()
    {
        if (eat_system.isPowerUpActive)
            return;

        if (isInvincible)
            return;

        StartCoroutine(
            StunCoroutine()
        );
    }


    public void stun_player()
    {
        if (isStunned ||
            isInvincible ||
            eat_system.isPowerUpActive)
            return;

        StartCoroutine(
            StunCoroutine()
        );
    }


    private IEnumerator StunCoroutine()
    {
        juice.PlayJuice();

        isStunned = true;

        anim.SetBool(
            "Stun",
            true
        );

        rb.linearVelocity =
            Vector2.zero;

        yield return new WaitForSeconds(
            stunDuration
        );

        anim.SetBool(
            "Stun",
            false
        );

        isStunned = false;
    }


    // =========================================================
    // LOW HEALTH
    // =========================================================

    private void HandleLowHealthBlink()
    {
        if (health == null)
            return;

        if (health.current_health == 1)
        {
            if (lowHealthCoroutine == null)
            {
                lowHealthCoroutine =
                    StartCoroutine(
                        LowHealthBlink()
                    );
            }
        }
        else
        {
            if (lowHealthCoroutine != null)
            {
                StopCoroutine(
                    lowHealthCoroutine
                );

                lowHealthCoroutine = null;
            }

            spriteRenderer.material =
                normalMaterial;
        }
    }


    private IEnumerator LowHealthBlink()
    {
        bool toggle = false;

        while (health.current_health == 1)
        {
            spriteRenderer.material =
                toggle
                    ? normalMaterial
                    : lowHealthMaterial;

            toggle = !toggle;

            yield return new WaitForSeconds(
                lowHealthBlinkRate
            );
        }

        spriteRenderer.material =
            normalMaterial;

        lowHealthCoroutine = null;
    }


    // =========================================================
    // DAMAGE
    // =========================================================

    public void TakeDamage(
        int damage,
        Vector2 ejection_power,
        Vector3 sourcePosition)
    {
        if (!canTakeDamage ||
            isInvincible ||
            burning)
            return;

        if (isFrozen ||
            eat_system.isPowerUpActive)
            return;

        Invoke(
            "delay_hit",
            hitFreezeTime
        );

        anim.SetBool(
            "Stun",
            false
        );

        anim.SetBool(
            "Hit",
            true
        );


        if (isClimbing)
            StopClimbingJump();


        if (damage == 0)
        {
            bounce_sfx.PlayJuice();

            if (hitCoroutine != null)
                StopCoroutine(
                    hitCoroutine
                );

            hitCoroutine =
                StartCoroutine(
                    BounceWithKnockback(
                        sourcePosition,
                        ejection_power
                    )
                );
        }
        else
        {
            health.take_damage(
                damage
            );

            eat_system.take_damage();

            damage_sfx.PlayJuice();

            if (health.current_health > 0)
            {
                if (hitCoroutine != null)
                    StopCoroutine(
                        hitCoroutine
                    );

                hitCoroutine =
                    StartCoroutine(
                        HitFreezeWithKnockback(
                            sourcePosition,
                            ejection_power
                        )
                    );

                StartInvincibility(
                    invincibilityTime
                );
            }
            else
            {
                Die();
            }
        }

        isStunned = false;
    }


    private void delay_hit()
    {
        anim.SetBool(
            "Hit",
            false
        );
    }


    // =========================================================
    // BOUNCE
    // =========================================================

    private IEnumerator BounceWithKnockback(
        Vector3 sourcePosition,
        Vector2 power)
    {
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

        yield return new WaitForSeconds(
            0.1f
        );
    }


    // =========================================================
    // HIT FREEZE
    // =========================================================

    private IEnumerator HitFreezeWithKnockback(
        Vector3 sourcePosition,
        Vector2 power)
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
                hitKnockback.x *
                power.x,

                hitKnockback.y *
                transform.localScale.y *
                power.y
            );

        yield return new WaitForSeconds(
            0.1f
        );

        if (!isInvincible)
            canTakeDamage = true;
    }


    // =========================================================
    // LAVA
    // =========================================================

    public void LavaHit(
        Vector2 launchVelocity,
        float controlMultiplier,
        float controlTime)
    {
        burning = true;

        moveSpeed =
            base_speed;

        PowermoveSpeed =
            BasePowermoveSpeed;

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
            return;

        if (eat_system.isPowerUpActive)
            return;

        if (health.current_health == 0)
            return;

        anim.SetBool(
            "Stun",
            false
        );

        anim.SetBool(
            "Hit",
            true
        );

        isStunned = false;
        isFrozen = false;

        eat_system.take_damage();

        health.take_damage(1);

        if (health.current_health > 0)
        {
            if (hitCoroutine != null)
                StopCoroutine(
                    hitCoroutine
                );

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


    private IEnumerator LavaControlLock(
        float multiplier,
        float time)
    {
        moveSpeed *= multiplier;
        PowermoveSpeed *= multiplier;

        yield return new WaitForSeconds(
            time
        );

        yield return new WaitUntil(
            () =>
                (
                    IsAnimationPlaying(
                        "jump_idle"
                    ) &&
                    isGrounded &&
                    Mathf.Abs(
                        rb.linearVelocity.y
                    ) < 0.01f &&
                    rb.gravityScale > 0
                )
                ||
                isClimbing
        );

        moveSpeed =
            base_speed;

        PowermoveSpeed =
            BasePowermoveSpeed;

        burning = false;
    }


    // =========================================================
    // INVINCIBILITY
    // =========================================================

    public void TriggerInvincibility(
        float duration)
    {
        StartInvincibility(
            duration
        );

        Invoke(
            "delay_safe",
            invincibilityTime + 0.1f
        );
    }


    public void StartInvincibility(
        float duration)
    {
        if (invincibilityCoroutine != null)
        {
            StopCoroutine(
                invincibilityCoroutine
            );
        }

        invincibilityCoroutine =
            StartCoroutine(
                InvincibilityRoutine(
                    duration
                )
            );

        Invoke(
            "delay",
            0.15f
        );
    }


    private void delay()
    {
        anim.SetBool(
            "Hit",
            false
        );
    }


    private IEnumerator InvincibilityRoutine(
        float duration)
    {
        isInvincible = true;
        canTakeDamage = false;

        float elapsed = 0f;
        bool visible = true;

        while (elapsed < duration)
        {
            visible = !visible;

            spriteRenderer.enabled =
                visible;

            yield return new WaitForSecondsRealtime(
                0.1f
            );

            elapsed += 0.1f;
        }

        spriteRenderer.enabled = true;

        isInvincible = false;
        canTakeDamage = true;

        invincibilityCoroutine = null;
    }


    private void delay_safe()
    {
        spriteRenderer.enabled = true;
    }


    // =========================================================
    // POWERUP
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

        anim.SetTrigger(
            trigger
        );

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


    public void powerup()
    {
        anim.ResetTrigger(
            "Punch"
        );

        anim.SetBool(
            "Eat",
            false
        );

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


        if (!other.CompareTag("Climb"))
            return;

        canClimb = true;

        grillage =
            other.GetComponent<SC_grillage>();

        // -----------------------------------------------------
        // ACCROCHAGE AUTOMATIQUE APRÈS DROP
        // -----------------------------------------------------

        if (isDroppingThrough &&
            grillage != null)
        {
            StartClimbing();
        }
    }

    private void OnTriggerExit2D(
        Collider2D other)
    {


        if (!other.CompareTag("Climb"))
            return;

        canClimb = false;

        grillage = null;
    }


    private void OnTriggerStay2D(
        Collider2D other)
    {
        if (!other.CompareTag("Climb"))
            return;

        canClimb = true;

        if (grillage == null)
        {
            grillage =
                other.GetComponent<SC_grillage>();
        }

        // -----------------------------------------------------
        // ACCROCHAGE AUTOMATIQUE APRÈS DROP
        // -----------------------------------------------------

        if (isDroppingThrough &&
            grillage != null)
        {
            StartClimbing();
        }
    }



    // =========================================================
    // TRY START CLIMBING
    // =========================================================

    private void TryStartClimbing(
        Vector2 input)
    {
        if (!canClimb ||!climb_delay ||
            isClimbing ||
            grillage == null ||
            climbReattachTimer > 0f)
            return;


        // -----------------------------------------------------
        // DOIT ÊTRE AU SOL ?
        // -----------------------------------------------------

        if (!grillage.canAttachInAir &&
            !isGrounded)
            return;


        // -----------------------------------------------------
        // LIMITE HAUTE
        // -----------------------------------------------------

        if (grillage.topClimbLimit != null &&
            grillage.topClimbLimit.position.y <
            transform.position.y)
            return;


        // -----------------------------------------------------
        // DIRECTION D'ACCROCHAGE
        // -----------------------------------------------------

        if (input.y < climbAttachUpThreshold)
            return;


        // -----------------------------------------------------
        // ÉCHELLE
        // -----------------------------------------------------

        if (Mathf.Abs(input.x) >
            input.y *
            climbAttachDiagonalLimit)
            return;


        StartClimbing();
    }


    // =========================================================
    // START CLIMBING
    // =========================================================

    private void StartClimbing()
    {
        if (!canClimb || !climb_delay ||
            grillage == null ||
            climbReattachTimer > 0f)
            return;
        collision.isTrigger = true;
        isClimbing = true;

        // -----------------------------------------------------
        // POSITION X SUR L'ÉCHELLE
        // -----------------------------------------------------

        if (grillage.isLadder)
        {
            float ladderX;

            if (grillage.box != null)
            {
                ladderX =
                    grillage.box.bounds.center.x;
            }
            else
            {
                ladderX =
                    (
                        grillage.minX +
                        grillage.maxX
                    ) / 2f;
            }

            rb.position =
                new Vector2(
                    ladderX,
                    rb.position.y
                );
        }

        // -----------------------------------------------------
        // PHYSIQUE
        // -----------------------------------------------------

        rb.gravityScale = 0f;
        rb.linearVelocity =
            Vector2.zero;

        anim.SetBool(
            "Climb",
            true
        );

        // Le drop est terminé dès que le joueur
        // est effectivement accroché.
        isDroppingThrough = false;

        if (dropThroughCoroutine != null)
        {
            StopCoroutine(
                dropThroughCoroutine
            );

            dropThroughCoroutine = null;
        }
    }


    // =========================================================
    // STOP CLIMBING
    // =========================================================

    private void StopClimbing()
    {
        if (!isClimbing)
            return;
        collision.isTrigger = false;
        Invoke("delay_climb", 0.5f);
        climb_delay = false;

        isClimbing = false;

        rb.gravityScale =
            base_gravity;

        grillage = null;
        canClimb = false;
        rb.linearVelocity =
            Vector2.zero;

        grid.Stop();

        anim.SetBool(
            "Climb",
            false
        );
    }


    // =========================================================
    // STOP CLIMBING + JUMP
    // =========================================================

    private void StopClimbingJump()
    {
        if (!isClimbing)
            return;
        Invoke("delay_climb",0.5f);
        grillage = null;
        canClimb = false;
        isClimbing = false;
        collision.isTrigger = false;

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
    void delay_climb()
    {
        climb_delay = true;
        grillage = null;
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
    // DIE
    // =========================================================

    public void Die()
    {
        canMove = false;

        if (hitCoroutine != null)
            StopCoroutine(
                hitCoroutine
            );

        if (dropThroughCoroutine != null)
            StopCoroutine(
                dropThroughCoroutine
            );

        rb.linearVelocity =
            Vector2.zero;

        GetComponent<SortingGroup>()
            .sortingLayerName = "UI";

        rb.constraints =
            RigidbodyConstraints2D.FreezeAll;

        rb.gravityScale = 0;

        isFrozen = true;

        anim.SetBool(
            "Die",
            true
        );

        anim.SetBool(
            "Eat",
            false
        );

        eat_system.eating_sfx.Stop();

        game_over_screen.SetActive(true);

        SC_music_manager.instance.stop_music();

        collider.enabled = false;

        die.PlayJuice();

        knockbackVelocity =
            Vector2.zero;

        externalVelocity =
            Vector2.zero;

        Time.timeScale = 0;
    }


    // =========================================================
    // REVIVE
    // =========================================================

    public void Revive()
    {
        Time.timeScale = 1f;

        normal.SetActive(true);
        transformed.SetActive(false);

        StopAllCoroutines();

        isFrozen = false;
        isStunned = false;
        isInvincible = false;
        burning = false;
        canTakeDamage = true;

        isClimbing = false;
        canClimb = false;
        grillage = null;

        climbReattachTimer = 0f;
        isDroppingThrough = false;
        dropThroughCoroutine = null;

        externalVelocity =
            Vector2.zero;

        knockbackVelocity =
            Vector2.zero;

        rb.gravityScale =
            base_gravity;

        GetComponent<SortingGroup>()
            .sortingLayerName = "Default";

        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;
        isJumping = false;

        if (health != null)
            health.revive();

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
        );

        anim.SetBool(
            "Hit",
            false
        );

        anim.SetTrigger(
            "Start"
        );

        transform.localScale =
            Vector3.one;
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (damageCheck != null)
        {
            Gizmos.color =
                Color.red;

            Gizmos.DrawWireSphere(
                damageCheck.position,
                damageRadius
            );
        }
    }


    // =========================================================
    // SCREEN WRAP
    // =========================================================

    private void LateUpdate()
    {
        float x =
            transform.position.x;


        if (x > limit.y - 0.2f)
        {
            ghost.gameObject.SetActive(
                true
            );

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
        else if (x < limit.x + 0.2f)
        {
            ghost.gameObject.SetActive(
                true
            );

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
            ghost.gameObject.SetActive(
                false
            );
        }


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
}
