
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
    private Coroutine knockbackInvincibilityCoroutine;

    public bool climb_delay = true;

    [Header("Power Up Stats")]
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

    [Header("Knockback Invincibility")]
    public float knockbackInvincibilityTime = 0.1f;
    private bool isKnockbackInvincible;

    [Header("Coyote Time")]
    public float coyoteTime = 0.1f;
    private float coyoteTimeCounter;

    [Header("Jump Input Buffer")]
    public float jumpBufferTime = 0.1f;
    [HideInInspector] public float jumpBufferCounter;

    private float jumpTimeCounter;
    private bool isJumping;
    private bool jumpHeld;

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
    public LayerMask platformLayer;
    public float dropThroughTime = 0.25f;
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

    [Header("Ground Detection Fix")]
    [Range(0.1f, 1f)]
    public float groundCheckWidthMultiplier = 0.9f;

    public float groundCheckHeight = 0.12f;
    public float groundCheckYOffset = 0.03f;

    [Tooltip("Distance of the additional ground raycasts.")]
    public float groundRaycastDistance = 0.15f;

    [Range(0f, 0.5f)]
    public float groundRaycastSideInset = 0.05f;

    [Header("Fall")]
    public string fallAnimName = "Fall";
    public bool playLandSoundAfterFall = true;

    private bool wasGroundedBeforeCheck;
    private bool isFalling;
    private bool fallWasTriggeredByJump;
    private bool landingHandled;

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
    public SC_juiciness start;

    public AudioSource grid;
    public AudioSource run;

    private Collider2D hit;
    private sc_health_system health;

    public Vector2 moveInput;
    public bool isGrounded;
    public bool wasGrounded;
    public bool isFrozen;

    // =========================================================
    // AWAKE / ENABLE / DISABLE
    // =========================================================

    private void Awake()
    {
        instance = this;
        base_speed = moveSpeed;
        BasePowermoveSpeed = PowermoveSpeed;
    }

    private void OnEnable()
    {
        if (Jump != null)
        {
            Jump.action.Enable();
            Jump.action.performed += OnJumpStarted;
            Jump.action.canceled += OnJumpReleased;
        }

        if (Move != null)
            Move.action.Enable();

        SC_scursorManager.instance.disable_cursor();
    }

    private void OnDisable()
    {
        if (Jump != null)
        {
            Jump.action.performed -= OnJumpStarted;
            Jump.action.canceled -= OnJumpReleased;
        }

        if (Move != null)
            Move.action.Disable();
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        canTakeDamage = true;
        climb_delay = true;
        health = sc_health_system.instance;

        if (normal != null)
            normal.SetActive(true);

        if (transformed != null)
            transformed.SetActive(false);

        base_gravity = rb.gravityScale;

        isFalling = false;
        fallWasTriggeredByJump = false;
        wasGrounded = false;
        wasGroundedBeforeCheck = false;
        landingHandled = false;

        if (spriteRenderer != null && normalMaterial != null)
            spriteRenderer.material = normalMaterial;
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (SC_level_master.instance != null)
            limit = SC_level_master.instance.limits;

        CheckStun();
        HandleLowHealthBlink();

        if (climbReattachTimer > 0f)
        {
            climbReattachTimer -= Time.deltaTime;
            climbReattachTimer = Mathf.Max(0f, climbReattachTimer);
        }

        if (isStunned)
        {
            moveInput = Vector2.zero;
            return;
        }

        // INPUT
        if (!isFrozen &&
            canMove &&
            !eat_system.isEating &&
            Time.timeScale != 0f)
        {
            Vector2 input = Move != null
                ? Move.action.ReadValue<Vector2>()
                : Vector2.zero;

            moveInput = new Vector2(
                Mathf.Abs(input.x) > 0.2f ? Mathf.Sign(input.x) : 0f,
                Mathf.Abs(input.y) > 0.2f ? Mathf.Sign(input.y) : 0f
            );

            if (!isClimbing &&
                !isDroppingThrough &&
                isGrounded &&
                input.y < -0.5f &&
                canClimb &&
                grillage != null)
            {
                StartDropThrough();
            }

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

        // GROUND CHECK
        bool previousGrounded = wasGroundedBeforeCheck;

        bool detectedGround = CheckGroundReliable();

        // Ne pas détecter un atterrissage pendant une montée.
        bool movingTowardGround =
            (rb.gravityScale >= 0f && rb.linearVelocity.y <= 0.05f) ||
            (rb.gravityScale < 0f && rb.linearVelocity.y >= -0.05f);

        isGrounded =
            detectedGround &&
            !isClimbing &&
            !isDroppingThrough &&
            movingTowardGround;

        // COYOTE TIME
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter = Mathf.Max(
                0f,
                coyoteTimeCounter - Time.deltaTime
            );
        }

        // LANDING
        // Une seule exécution par contact avec le sol.
        if (isGrounded && !previousGrounded && !landingHandled)
        {
            HandleLanding();
            landingHandled = true;
        }

        // Réarmer lorsque le joueur quitte le sol.
        if (!isGrounded)
        {
            landingHandled = false;
        }

        // JUMP BUFFER
        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter = Mathf.Max(
                0f,
                jumpBufferCounter - Time.deltaTime
            );
        }

        // SAUT EN ATTENTE
        if (jumpBufferCounter > 0f &&
            isGrounded &&
            !isClimbing &&
            !isJumping)
        {
            TryJump();
        }

        // FALL ANIMATION
        if (!isGrounded &&
            previousGrounded &&
            !isJumping &&
            !isClimbing &&
            !isDroppingThrough &&
            ((rb.gravityScale >= 0f && rb.linearVelocity.y < 0f) ||
             (rb.gravityScale < 0f && rb.linearVelocity.y > 0f)))
        {
            StartFallAnimation();
        }

        // ANIMATIONS AU SOL ET SUR LES GRILLES
        if (isClimbing)
        {
            StopFallAnimation();

            if (moveInput.sqrMagnitude > 0.01f)
            {
                if (grid != null && !grid.isPlaying)
                    grid.Play();

                anim.SetBool("Run", true);
            }
            else
            {
                if (grid != null)
                    grid.Stop();

                anim.SetBool("Run", false);
            }
        }
        else if (isGrounded)
        {
            StopFallAnimation();

            if (Mathf.Abs(moveInput.x) > 0.1f)
            {
                if (run != null && !run.isPlaying)
                    run.Play();

                anim.SetBool("Run", true);
            }
            else
            {
                if (run != null)
                    run.Stop();

                anim.SetBool("Run", false);
            }
        }
        else
        {
            if (run != null)
                run.Stop();

            anim.SetBool("Run", false);

            bool movingAgainstGravity =
                (rb.gravityScale >= 0f && rb.linearVelocity.y < 0f) ||
                (rb.gravityScale < 0f && rb.linearVelocity.y > 0f);

            if (!isJumping &&
                !isClimbing &&
                !isDroppingThrough &&
                movingAgainstGravity)
            {
                StartFallAnimation();
            }
        }

        // SAUT VARIABLE
        if (isJumping && jumpHeld)
        {
            if (jumpTimeCounter > 0f)
            {
                float jumpPower = eat_system.isPowerUpActive
                    ? PowerJump
                    : jumpForce;

                float direction = rb.gravityScale >= 0f ? 1f : -1f;

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

        // DIRECTION DU PERSONNAGE
        float yScale = transform.localScale.y;

        if (moveInput.x > 0f)
            transform.localScale = new Vector3(1f, yScale, 1f);
        else if (moveInput.x < 0f)
            transform.localScale = new Vector3(-1f, yScale, 1f);

        wasGrounded = isGrounded;
        wasGroundedBeforeCheck = isGrounded;
    }

    // =========================================================
    // RELIABLE GROUND DETECTION
    // =========================================================

    private bool CheckGroundReliable()
    {
        if (groundCheckBottom == null)
            return false;

        float width = groundCheckRadius * 2f;
        float bottomY = groundCheckBottom.position.y;
        float centerX = groundCheckBottom.position.x;

        if (collider != null)
        {
            Bounds bounds = collider.bounds;

            width = bounds.size.x * groundCheckWidthMultiplier;
            centerX = bounds.center.x;

            // Supporte également la gravité inversée.
            bottomY = rb.gravityScale >= 0f
                ? bounds.min.y
                : bounds.max.y;
        }

        width = Mathf.Max(width, 0.05f);

        float direction = rb.gravityScale >= 0f ? -1f : 1f;

        Vector2 boxCenter = new Vector2(
            centerX,
            bottomY + direction * groundCheckYOffset
        );

        Vector2 boxSize = new Vector2(
            width,
            Mathf.Max(groundCheckHeight, 0.02f)
        );

        // Première vérification : zone couvrant les pieds.
        Collider2D ground = Physics2D.OverlapBox(
            boxCenter,
            boxSize,
            0f,
            groundLayer
        );

        if (ground != null && ground != collider)
            return true;

        // Deuxième vérification : raycasts répartis sous
        // le collider, notamment près des deux extrémités.
        float halfWidth = width * 0.5f;
        float inset = Mathf.Min(
            groundRaycastSideInset,
            halfWidth * 0.5f
        );

        float leftX = centerX - halfWidth + inset;
        float rightX = centerX + halfWidth - inset;

        float rayDistance = Mathf.Max(
            groundRaycastDistance,
            groundCheckYOffset + groundCheckHeight
        );

        Vector2 originCenter = new Vector2(centerX, bottomY);
        Vector2 originLeft = new Vector2(leftX, bottomY);
        Vector2 originRight = new Vector2(rightX, bottomY);

        Vector2 rayDirection = rb.gravityScale >= 0f
            ? Vector2.down
            : Vector2.up;

        if (HasGroundRay(originCenter, rayDirection, rayDistance))
            return true;

        if (HasGroundRay(originLeft, rayDirection, rayDistance))
            return true;

        if (HasGroundRay(originRight, rayDirection, rayDistance))
            return true;

        return false;
    }

    private bool HasGroundRay(
        Vector2 origin,
        Vector2 direction,
        float distance)
    {
        RaycastHit2D rayHit = Physics2D.Raycast(
            origin,
            direction,
            distance,
            groundLayer
        );

        return rayHit.collider != null &&
               rayHit.collider != collider;
    }

    // =========================================================
    // DROP THROUGH PLATFORM
    // =========================================================

    private void StartDropThrough()
    {
        if (dropThroughCoroutine != null)
            StopCoroutine(dropThroughCoroutine);

        dropThroughCoroutine = StartCoroutine(DropThroughPlatforms());
    }

    private IEnumerator DropThroughPlatforms()
    {
        if (isDroppingThrough)
            yield break;

        isDroppingThrough = true;
        isGrounded = false;
        wasGroundedBeforeCheck = false;
        landingHandled = false;

        Collider2D[] playerColliders =
            GetComponentsInChildren<Collider2D>();

        Collider2D[] platforms = Physics2D.OverlapCircleAll(
            transform.position,
            1f,
            platformLayer
        );

        foreach (Collider2D playerCollider in playerColliders)
        {
            foreach (Collider2D platform in platforms)
            {
                if (playerCollider != null && platform != null)
                {
                    Physics2D.IgnoreCollision(
                        playerCollider,
                        platform,
                        true
                    );
                }
            }
        }

        float direction = rb.gravityScale >= 0f ? -1f : 1f;

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            direction * Mathf.Abs(dropThroughVelocity)
        );

        yield return new WaitForSeconds(dropThroughTime);

        foreach (Collider2D playerCollider in playerColliders)
        {
            foreach (Collider2D platform in platforms)
            {
                if (playerCollider != null && platform != null)
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
        if (Time.timeScale == 0f)
            return;

        if (isClimbing)
        {
            if (grillage == null)
            {
                StopClimbing();
                return;
            }

            Vector2 climbInput = Move != null
                ? Move.action.ReadValue<Vector2>()
                : Vector2.zero;

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
                climbVelocity = climbInput * climbSpeed;
            }

            Vector2 nextPosition = rb.position +
                climbVelocity * Time.fixedDeltaTime;

            nextPosition = grillage.ClampPosition(nextPosition);

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
                    nextPosition.y = reachedBottom
                        ? grillage.minY
                        : grillage.maxY;

                    nextPosition.x = rb.position.x;
                    rb.position = nextPosition;

                    StopClimbing();
                    return;
                }
            }

            rb.linearVelocity = climbVelocity;
            rb.position = nextPosition;
            return;
        }

        float playerHorizontalVelocity = 0f;

        if (!isFrozen && canMove && !eat_system.isEating)
        {
            playerHorizontalVelocity = moveInput.x *
                (eat_system.isPowerUpActive
                    ? PowermoveSpeed
                    : moveSpeed);
        }

        float knockbackX = knockbackVelocity.x;
        float verticalVelocity = rb.linearVelocity.y;

        if (knockbackVelocity.y != 0f)
        {
            verticalVelocity = knockbackVelocity.y;
            knockbackVelocity.y = 0f;
        }

        float finalX = playerHorizontalVelocity +
            externalVelocity.x +
            knockbackX;

        float finalY = verticalVelocity + externalVelocity.y;

        rb.linearVelocity = new Vector2(finalX, finalY);

        knockbackVelocity.x = Mathf.Lerp(
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
        if (Time.timeScale == 0f)
            return;

        if (!canJump || isStunned || isFrozen || !canMove)
            return;

        if (eat_system.isEating)
            return;

        // SAUT DEPUIS UNE GRILLE
        if (isClimbing)
        {
            if (grillage == null)
                return;

            if (grillage.isLadder)
            {
                StopClimbing();
            }
            else
            {
                StopClimbingJump();
                PerformJump();
            }

            return;
        }

        // SAUT AERIEN EN MODE INVINCIBLE
        if (isInvincible)
        {
            PerformJump();
            return;
        }

        // SAUT NORMAL
        if (rb.linearVelocity.y < 0.5f)
        {
            bool canPerformJump =
                isGrounded ||
                coyoteTimeCounter > 0f ||
                eat_system.isPowerUpActive;

            if (!canPerformJump)
                return;

            PerformJump();
        }
    }

    private void PerformJump()
    {
        StopFallAnimation();

        jumpTimeCounter = maxJumpTime;
        isJumping = jumpHeld;

        float jumpPower = eat_system.isPowerUpActive
            ? PowerJump
            : jumpForce;

        float direction = rb.gravityScale >= 0f ? 1f : -1f;

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            jumpPower * direction
        );

        // Réarmer le prochain atterrissage.
        landingHandled = false;

        anim.ResetTrigger("Fall");
        anim.ResetTrigger("Land");
        anim.SetBool("Run", false);
        anim.SetTrigger("Jump");

        if (run != null)
            run.Stop();

        if (grid != null)
            grid.Stop();

        if (jump != null)
            jump.PlayJuice();

        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;

        isGrounded = false;
        wasGroundedBeforeCheck = false;
        wasGrounded = false;
    }

    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        if (Time.timeScale == 0f)
            return;

        if (!canJump || isStunned || isFrozen ||
            !canMove || eat_system.isEating)
            return;

        jumpHeld = true;
        jumpBufferCounter = jumpBufferTime;

        if (isInvincible)
        {
            TryJump();
            return;
        }

        if (isGrounded || coyoteTimeCounter > 0f || isClimbing)
            TryJump();
    }

    private void OnJumpReleased(InputAction.CallbackContext context)
    {
        jumpHeld = false;
        isJumping = false;
        jumpTimeCounter = 0f;
    }

    // =========================================================
    // FALL / LANDING
    // =========================================================

    private void StartFallAnimation()
    {
        if (isFalling)
            return;

        isFalling = true;
        fallWasTriggeredByJump = isJumping;

        if (run != null)
            run.Stop();

        if (grid != null)
            grid.Stop();

        anim.SetBool("Run", false);
        anim.ResetTrigger("Land");
        anim.SetTrigger("Fall");
    }

    private void StopFallAnimation()
    {
        isFalling = false;
        fallWasTriggeredByJump = false;

        anim.ResetTrigger("Fall");
    }

    private void HandleLanding()
    {
        isJumping = false;
        jumpHeld = false;
        jumpTimeCounter = 0f;
        jumpBufferCounter = 0f;

        isFalling = false;
        fallWasTriggeredByJump = false;

        PowermoveSpeed = BasePowermoveSpeed;
        canClimb = true;

        anim.ResetTrigger("Jump");
        anim.ResetTrigger("Fall");
        anim.SetBool("Run", false);

        if (run != null)
            run.Stop();

        if (grid != null)
            grid.Stop();

        moveSpeed = base_speed;
        burning = false;

        anim.ResetTrigger("Land");
        anim.SetTrigger("Land");

        if (playLandSoundAfterFall && land != null)
            land.PlayJuice();
    }

    public bool IsAnimationPlaying(string animationName)
    {
        AnimatorStateInfo stateInfo =
            anim.GetCurrentAnimatorStateInfo(0);

        return stateInfo.IsName(animationName);
    }

    // =========================================================
    // STUN
    // =========================================================

    private void CheckStun()
    {
        if (isStunned || isInvincible || eat_system.isPowerUpActive)
            return;

        Collider2D detectedHit = Physics2D.OverlapCircle(
            damageCheck.position,
            damageRadius,
            stunLayer
        );

        if (detectedHit != null)
            StartCoroutine(StunCoroutine());
    }

    public void Stun()
    {
        if (eat_system.isPowerUpActive || isInvincible)
            return;

        StartCoroutine(StunCoroutine());
    }

    public void stun_player()
    {
        if (isStunned || isInvincible || eat_system.isPowerUpActive)
            return;

        StartCoroutine(StunCoroutine());
    }

    private IEnumerator StunCoroutine()
    {
        if (isStunned)
            yield break;

        if (juice != null)
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

    private void HandleLowHealthBlink()
    {
        if (health == null)
            return;

        if (health.current_health == 1)
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

            if (spriteRenderer != null && normalMaterial != null)
                spriteRenderer.material = normalMaterial;
        }
    }

    private IEnumerator LowHealthBlink()
    {
        bool toggle = false;

        while (health != null && health.current_health == 1)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.material = toggle
                    ? normalMaterial
                    : lowHealthMaterial;
            }

            toggle = !toggle;

            yield return new WaitForSeconds(lowHealthBlinkRate);
        }

        if (spriteRenderer != null && normalMaterial != null)
            spriteRenderer.material = normalMaterial;

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
        SC_icecream_eat_system.instance.CancelEat();

        if (damage == 0 && isKnockbackInvincible)
            return;

        if (!canTakeDamage || isInvincible || burning)
            return;

        if (isFrozen || eat_system.isPowerUpActive)
            return;

        Invoke(nameof(delay_hit), hitFreezeTime);

        anim.SetBool("Stun", false);
        anim.SetBool("Hit", true);

        if (isClimbing)
            StopClimbingJump();

        if (damage == 0)
        {
            if (bounce_sfx != null)
                bounce_sfx.PlayJuice();

            if (knockbackInvincibilityCoroutine != null)
                StopCoroutine(knockbackInvincibilityCoroutine);

            knockbackInvincibilityCoroutine =
                StartCoroutine(KnockbackInvincibility());

            if (hitCoroutine != null)
                StopCoroutine(hitCoroutine);

            hitCoroutine = StartCoroutine(
                BounceWithKnockback(sourcePosition, ejection_power)
            );
        }
        else
        {
            health.take_damage(damage);
            eat_system.take_damage();

            if (damage_sfx != null)
                damage_sfx.PlayJuice();

            if (health.current_health > 0)
            {
                if (hitCoroutine != null)
                    StopCoroutine(hitCoroutine);

                hitCoroutine = StartCoroutine(
                    HitFreezeWithKnockback(sourcePosition, ejection_power)
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

    private void delay_hit()
    {
        anim.SetBool("Hit", false);
    }

    // =========================================================
    // BOUNCE / KNOCKBACK INVINCIBILITY
    // =========================================================

    private IEnumerator BounceWithKnockback(
        Vector3 sourcePosition,
        Vector2 power)
    {
        Vector2 direction =
            (transform.position - sourcePosition).normalized;

        knockbackVelocity = new Vector2(
            direction.x * hitKnockback.x * power.x,
            hitKnockback.y * transform.localScale.y * power.y
        );

        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator KnockbackInvincibility()
    {
        isKnockbackInvincible = true;

        yield return new WaitForSeconds(knockbackInvincibilityTime);

        isKnockbackInvincible = false;
        knockbackInvincibilityCoroutine = null;
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

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        yield return new WaitForSecondsRealtime(hitFreezeTime);

        rb.bodyType = RigidbodyType2D.Dynamic;
        isFrozen = false;

        Vector2 direction =
            (transform.position - sourcePosition).normalized;

        knockbackVelocity = new Vector2(
            direction.x * hitKnockback.x * power.x,
            hitKnockback.y * transform.localScale.y * power.y
        );

        yield return new WaitForSeconds(0.1f);

        if (!isInvincible)
            canTakeDamage = true;

        hitCoroutine = null;
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

        moveSpeed = base_speed;
        PowermoveSpeed = BasePowermoveSpeed;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = launchVelocity;

        StartCoroutine(
            LavaControlLock(controlMultiplier, controlTime)
        );

        anim.SetBool("Hit", true);
        Invoke(nameof(delay), 0.15f);

        if (isInvincible || !canTakeDamage)
            return;

        if (eat_system.isPowerUpActive)
            return;

        if (health.current_health == 0)
            return;

        anim.SetBool("Stun", false);

        isStunned = false;
        isFrozen = false;

        eat_system.take_damage();
        health.take_damage(1);

        if (health.current_health > 0)
        {
            if (hitCoroutine != null)
                StopCoroutine(hitCoroutine);

            StartInvincibility(invincibilityTime);
        }
        else
        {
            Die();
        }

        if (damage_lava_sfx != null)
            damage_lava_sfx.PlayJuice();
    }

    private IEnumerator LavaControlLock(float multiplier, float time)
    {
        moveSpeed *= multiplier;
        PowermoveSpeed *= multiplier;

        yield return new WaitForSeconds(time);

        yield return new WaitUntil(() =>
            (IsAnimationPlaying("jump_idle") &&
             isGrounded &&
             Mathf.Abs(rb.linearVelocity.y) < 0.01f &&
             rb.gravityScale > 0f) ||
            isClimbing
        );

        moveSpeed = base_speed;
        PowermoveSpeed = BasePowermoveSpeed;
        burning = false;
    }

    // =========================================================
    // INVINCIBILITY
    // =========================================================

    public void TriggerInvincibility(float duration)
    {
        StartInvincibility(duration);
        Invoke(nameof(delay_safe), duration + 0.1f);
    }

    public void StartInvincibility(float duration)
    {
        if (invincibilityCoroutine != null)
            StopCoroutine(invincibilityCoroutine);

        invincibilityCoroutine =
            StartCoroutine(InvincibilityRoutine(duration));

        Invoke(nameof(delay), 0.15f);
    }

    private void delay()
    {
        anim.SetBool("Hit", false);
    }

    private IEnumerator InvincibilityRoutine(float duration)
    {
        isInvincible = true;
        canTakeDamage = false;

        float elapsed = 0f;
        bool visible = true;

        while (elapsed < duration)
        {
            visible = !visible;

            if (spriteRenderer != null)
                spriteRenderer.enabled = visible;

            yield return new WaitForSecondsRealtime(0.1f);
            elapsed += 0.1f;
        }

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        isInvincible = false;
        canTakeDamage = true;

        invincibilityCoroutine = null;
    }

    private void delay_safe()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
    }

    // =========================================================
    // POWERUP
    // =========================================================

    private IEnumerator PowerupFreeze(bool isActivating)
    {
        isFrozen = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        string trigger = isActivating
            ? transformAnimTrigger
            : detransformAnimTrigger;

        anim.SetTrigger(trigger);

        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(transformFreezeTime);

        Time.timeScale = originalTimeScale;
        rb.bodyType = RigidbodyType2D.Dynamic;
        isFrozen = false;
    }

    public void powerup()
    {
        anim.ResetTrigger("Punch");
        anim.SetBool("Eat", false);

        isInvincible = true;

        if (transformation != null)
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

    // =========================================================
    // CLIMB TRIGGERS
    // =========================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Climb"))
            return;

        grillage = other.GetComponent<SC_grillage>();

        if (isDroppingThrough && grillage != null)
            StartClimbing();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Climb"))
            return;

        canClimb = false;
        grillage = null;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Climb"))
            return;

        canClimb = true;

        if (grillage == null)
            grillage = other.GetComponent<SC_grillage>();

        if (isDroppingThrough && grillage != null)
            StartClimbing();
    }

    // =========================================================
    // TRY START CLIMBING
    // =========================================================

    private void TryStartClimbing(Vector2 input)
    {
        if (!canClimb || !climb_delay ||
            isClimbing || grillage == null ||
            climbReattachTimer > 0f)
            return;

        if (!grillage.canAttachInAir && !isGrounded)
            return;

        if (grillage.topClimbLimit != null &&
            grillage.topClimbLimit.position.y < transform.position.y)
            return;

        if (input.y < climbAttachUpThreshold)
            return;

        if (Mathf.Abs(input.x) >
            input.y * climbAttachDiagonalLimit)
            return;

        StartClimbing();
    }

    // =========================================================
    // START CLIMBING
    // =========================================================

    private void StartClimbing()
    {
        if (!canClimb || !climb_delay ||
            grillage == null || climbReattachTimer > 0f)
            return;

        collision.isTrigger = true;

        StopFallAnimation();
        isClimbing = true;
        landingHandled = false;

        if (grillage.isLadder)
        {
            float ladderX;

            if (grillage.box != null)
                ladderX = grillage.box.bounds.center.x;
            else
                ladderX = (grillage.minX + grillage.maxX) / 2f;

            rb.position = new Vector2(ladderX, rb.position.y);
        }

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        anim.SetBool("Climb", true);

        isDroppingThrough = false;

        if (dropThroughCoroutine != null)
        {
            StopCoroutine(dropThroughCoroutine);
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

        Invoke(nameof(delay_climb), 0.5f);
        climb_delay = false;

        isClimbing = false;
        rb.gravityScale = base_gravity;

        grillage = null;
        canClimb = false;
        rb.linearVelocity = Vector2.zero;

        if (grid != null)
            grid.Stop();

        anim.SetBool("Climb", false);
        landingHandled = false;
        wasGroundedBeforeCheck = false;
    }

    private void StopClimbingJump()
    {
        if (!isClimbing)
            return;

        CancelInvoke(nameof(delay_climb));

        climb_delay = true;

        grillage = null;
        canClimb = false;
        isClimbing = false;
        collision.isTrigger = false;

        climbReattachTimer = climbReattachDelay;

        rb.gravityScale = base_gravity;
        rb.linearVelocity = Vector2.zero;

        if (grid != null)
            grid.Stop();

        anim.SetBool("Climb", false);

        landingHandled = false;
        wasGroundedBeforeCheck = false;
    }

    private void delay_climb()
    {
        climb_delay = true;
        grillage = null;
    }

    // =========================================================
    // FACE TARGET
    // =========================================================

    public void FaceTarget(Transform target)
    {
        if (target == null)
            return;

        float direction = target.position.x - transform.position.x;

        if (direction > 0f)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
        else if (direction < 0f)
        {
            transform.localScale = new Vector3(
                -Mathf.Abs(transform.localScale.x),
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
        eat_system.destroy_all_cream();

        if (hitCoroutine != null)
            StopCoroutine(hitCoroutine);

        if (dropThroughCoroutine != null)
            StopCoroutine(dropThroughCoroutine);

        rb.linearVelocity = Vector2.zero;

        GetComponent<SortingGroup>().sortingLayerName = "UI";

        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        rb.gravityScale = 0f;

        isFrozen = true;

        if (PlayerPrefs.GetInt("currentlevel") == 1)
        {
            SC_SaveManager.instance.AddAchievementProgress(
                AchievementType.Loose_level1, 1
            );
        }

        anim.SetBool("Die", true);
        anim.SetBool("Eat", false);

        eat_system.eating_sfx.Stop();
        game_over_screen.SetActive(true);

        SC_music_manager.instance.stop_music();

        collider.enabled = false;

        if (die != null)
            die.PlayJuice();

        knockbackVelocity = Vector2.zero;
        externalVelocity = Vector2.zero;

        Time.timeScale = 0f;
    }

    // =========================================================
    // REVIVE
    // =========================================================

    public void Revive()
    {
        isKnockbackInvincible = false;
        knockbackInvincibilityCoroutine = null;

        Time.timeScale = 1f;

        normal.SetActive(true);
        transformed.SetActive(false);

        StopAllCoroutines();

        invincibilityCoroutine = null;
        lowHealthCoroutine = null;
        hitCoroutine = null;
        dropThroughCoroutine = null;

        CancelInvoke();

        isFrozen = false;
        isStunned = false;
        isInvincible = false;
        burning = false;
        canTakeDamage = true;
        canMove = true;

        moveSpeed = base_speed;
        PowermoveSpeed = BasePowermoveSpeed;

        canClimb = false;
        isClimbing = false;
        grillage = null;

        climbReattachTimer = 0f;
        isDroppingThrough = false;

        externalVelocity = Vector2.zero;
        knockbackVelocity = Vector2.zero;

        rb.gravityScale = base_gravity;

        GetComponent<SortingGroup>().sortingLayerName = "Default";

        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;
        jumpTimeCounter = 0f;
        isJumping = false;
        jumpHeld = false;

        isFalling = false;
        fallWasTriggeredByJump = false;
        wasGrounded = false;
        wasGroundedBeforeCheck = false;
        isGrounded = false;
        landingHandled = false;

        if (health != null)
            health.revive();

        collider.enabled = true;
        spriteRenderer.enabled = true;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.angularVelocity = 0f;

        eat_system.ResetSystem();

        anim.SetBool("Die", false);
        anim.SetBool("Climb", false);
        anim.SetBool("Hit", false);
        anim.SetBool("Stun", false);
        anim.SetBool("Run", false);

        anim.ResetTrigger("Jump");
        anim.ResetTrigger("Fall");
        anim.ResetTrigger("Land");

        anim.SetTrigger("Start");

        transform.localScale = Vector3.one;
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (damageCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(damageCheck.position, damageRadius);
        }

        if (groundCheckBottom != null)
        {
            Gizmos.color = Color.green;

            float width = groundCheckRadius * 2f;
            float bottomY = groundCheckBottom.position.y;
            float centerX = groundCheckBottom.position.x;

            if (collider != null)
            {
                Bounds bounds = collider.bounds;

                width = bounds.size.x * groundCheckWidthMultiplier;
                centerX = bounds.center.x;

                bottomY = rb != null && rb.gravityScale < 0f
                    ? bounds.max.y
                    : bounds.min.y;
            }

            float direction = rb != null && rb.gravityScale < 0f
                ? 1f
                : -1f;

            Vector3 center = new Vector3(
                centerX,
                bottomY + direction * groundCheckYOffset,
                groundCheckBottom.position.z
            );

            Gizmos.DrawWireCube(
                center,
                new Vector3(
                    width,
                    groundCheckHeight,
                    0.01f
                )
            );

            float halfWidth = width * 0.5f;
            float inset = Mathf.Min(
                groundRaycastSideInset,
                halfWidth * 0.5f
            );

            Vector3 leftOrigin = new Vector3(
                centerX - halfWidth + inset,
                bottomY,
                groundCheckBottom.position.z
            );

            Vector3 rightOrigin = new Vector3(
                centerX + halfWidth - inset,
                bottomY,
                groundCheckBottom.position.z
            );

            Vector3 rayDirection = direction > 0f
                ? Vector3.up
                : Vector3.down;

            Gizmos.DrawLine(
                leftOrigin,
                leftOrigin + rayDirection * groundRaycastDistance
            );

            Gizmos.DrawLine(
                rightOrigin,
                rightOrigin + rayDirection * groundRaycastDistance
            );
        }
    }

    // =========================================================
    // SCREEN WRAP
    // =========================================================

    private void LateUpdate()
    {
        if (ghost == null)
            return;

        float x = transform.position.x;

        if (x > limit.y - 0.2f)
        {
            ghost.gameObject.SetActive(true);
            ghost.localScale = transform.localScale;

            float distance = limit.y - x;
            float ghostX = limit.x - distance;

            ghost.position = new Vector3(
                ghostX,
                transform.position.y,
                transform.position.z
            );
        }
        else if (x < limit.x + 0.2f)
        {
            ghost.gameObject.SetActive(true);
            ghost.localScale = transform.localScale;

            float distance = x - limit.x;
            float ghostX = limit.y + distance;

            ghost.position = new Vector3(
                ghostX,
                transform.position.y,
                transform.position.z
            );
        }
        else
        {
            ghost.gameObject.SetActive(false);
        }

        if (x > limit.y)
        {
            transform.position = new Vector3(
                limit.x,
                transform.position.y,
                transform.position.z
            );

            if (rb.linearVelocity.x > 0f)
            {
                rb.linearVelocity = new Vector2(
                    -Mathf.Abs(rb.linearVelocity.x),
                    rb.linearVelocity.y
                );
            }
        }
        else if (x < limit.x)
        {
            transform.position = new Vector3(
                limit.y,
                transform.position.y,
                transform.position.z
            );

            if (rb.linearVelocity.x < 0f)
            {
                rb.linearVelocity = new Vector2(
                    Mathf.Abs(rb.linearVelocity.x),
                    rb.linearVelocity.y
                );
            }
        }
    }
}
