
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

    [Header("Knockback Invincibility")]
    public float knockbackInvincibilityTime = 0.1f;
    private bool isKnockbackInvincible;
    private Coroutine knockbackInvincibilityCoroutine;

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

    [Header("Fall")]
    public string fallAnimName = "Fall";
    public bool playLandSoundAfterFall = true;

    private bool wasGroundedBeforeCheck;
    private bool isFalling;
    private bool fallWasTriggeredByJump;
    private bool landPending;

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
    }

    private void OnDisable()
    {
        if (Jump != null)
        {
            Jump.action.performed -= OnJumpStarted;
            Jump.action.canceled -= OnJumpReleased;
        }
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

        isFalling = false;
        fallWasTriggeredByJump = false;
        wasGrounded = false;
        wasGroundedBeforeCheck = false;

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

        if (climbReattachTimer > 0f)
        {
            climbReattachTimer -= Time.deltaTime;

            if (climbReattachTimer < 0f)
                climbReattachTimer = 0f;
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
            Time.timeScale != 0)
        {
            Vector2 input = Move.action.ReadValue<Vector2>();

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
        // Important : vérifier le sol AVANT de traiter le buffer.
        bool previousGrounded = wasGroundedBeforeCheck;

        isGrounded = Physics2D.OverlapCircle(
            groundCheckBottom.position,
            groundCheckRadius,
            groundLayer
        );

        // COYOTE TIME
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;

            if (coyoteTimeCounter < 0f)
                coyoteTimeCounter = 0f;
        }

        // LANDING
        // Réinitialiser l'ancien saut avant de traiter le buffer.
        if (!previousGrounded &&
            isGrounded &&
            !isClimbing &&
            rb.gravityScale != 0f)
        {
            HandleLanding();
        }

        // JUMP BUFFER
        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;

            if (jumpBufferCounter < 0f)
                jumpBufferCounter = 0f;
        }

        // Si une demande de saut est en attente, on la déclenche
        // dès que le personnage touche réellement le sol.
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
            rb.linearVelocity.y < 0f)
        {
            StartFallAnimation();
        }

        // ANIMATIONS
        if (isClimbing)
        {
            StopFallAnimation();

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
        else if (isGrounded)
        {
            StopFallAnimation();

            if (Mathf.Abs(moveInput.x) > 0.1f)
            {
                if (!run.isPlaying)
                    run.Play();

                anim.SetBool("Run", true);
            }
            else
            {
                run.Stop();
                anim.SetBool("Run", false);
            }
        }
        else
        {
            run.Stop();
            anim.SetBool("Run", false);

            if (!isJumping &&
                !isClimbing &&
                rb.linearVelocity.y < 0f &&
                rb.gravityScale > 0f)
            {
                StartFallAnimation();
            }

            if (!isJumping &&
                !isClimbing &&
                rb.linearVelocity.y > 0f &&
                rb.gravityScale < 0f)
            {
                StartFallAnimation();
            }
        }

        // Fallback pour les animations de saut existantes.
        if ((IsAnimationPlaying("jump_idle") ||
             IsAnimationPlaying("Jump")) &&
            isGrounded &&
            Mathf.Abs(rb.linearVelocity.y) < 0.01f)
        {
            if (rb.gravityScale > 0f && rb.linearVelocity.y < 0.5f)
            {
                anim.SetTrigger("Land");
                moveSpeed = base_speed;
                burning = false;

                if (land != null)
                    land.PlayJuice();
            }
            else if (rb.gravityScale < 0f && rb.linearVelocity.y > -0.5f)
            {
                anim.SetTrigger("Land");
                moveSpeed = base_speed;
                burning = false;

                if (land != null)
                    land.PlayJuice();
            }
        }

        wasGrounded = isGrounded;
        wasGroundedBeforeCheck = isGrounded;

        // FACING
        float yScale = transform.localScale.y;

        if (moveInput.x > 0)
            transform.localScale = new Vector3(1, yScale, 1);
        else if (moveInput.x < 0)
            transform.localScale = new Vector3(-1, yScale, 1);

        // VARIABLE JUMP
        // La hauteur dépend du maintien du bouton.
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

        Collider2D[] playerColliders = GetComponentsInChildren<Collider2D>();

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
                    Physics2D.IgnoreCollision(playerCollider, platform, true);
                }
            }
        }

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            -dropThroughVelocity
        );

        yield return new WaitForSeconds(dropThroughTime);

        foreach (Collider2D playerCollider in playerColliders)
        {
            foreach (Collider2D platform in platforms)
            {
                if (playerCollider != null && platform != null)
                {
                    Physics2D.IgnoreCollision(playerCollider, platform, false);
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

        if (isClimbing)
        {
            if (grillage == null)
            {
                StopClimbing();
                return;
            }

            Vector2 climbInput = Move.action.ReadValue<Vector2>();

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
        if (Time.timeScale == 0)
            return;

        if (!canJump || isStunned || isFrozen || !canMove)
            return;

        if (eat_system.isEating)
            return;

        // SAUT DEPUIS LA GRILLE
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

                StopFallAnimation();

                jumpTimeCounter = maxJumpTime;
                isJumping = jumpHeld;

                float jumpPower = eat_system.isPowerUpActive
                    ? PowerJump
                    : jumpForce;

                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    jumpPower
                );

                anim.SetTrigger("Jump");

                if (jump != null)
                    jump.PlayJuice();

                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
            }

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

            StopFallAnimation();

            // Initialiser la durée uniquement au démarrage effectif.
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

            anim.SetTrigger("Jump");

            if (jump != null)
                jump.PlayJuice();

            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }
    }

    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        if (Time.timeScale == 0)
            return;

        if (!canJump || isStunned || isFrozen ||
            !canMove || eat_system.isEating)
            return;

        jumpHeld = true;
        jumpBufferCounter = jumpBufferTime;

        // Déclenche immédiatement si le saut est autorisé.
        // Sinon, le buffer attend l'atterrissage.
        if (isGrounded || coyoteTimeCounter > 0f || isClimbing)
            TryJump();
    }

    private void OnJumpReleased(InputAction.CallbackContext context)
    {
        jumpHeld = false;
        isJumping = false;
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

        run.Stop();
        grid.Stop();

        anim.SetBool("Run", false);
        anim.ResetTrigger("Land");
        anim.SetTrigger("Fall");
    }

    private void StopFallAnimation()
    {
        if (!isFalling)
            return;

        isFalling = false;
        fallWasTriggeredByJump = false;
    }

    private void HandleLanding()
    {
        // Ne pas effacer jumpBufferCounter ici :
        // le buffer doit pouvoir déclencher le saut à l'atterrissage.
        isJumping = false;
        jumpTimeCounter = 0f;

        isFalling = false;
        fallWasTriggeredByJump = false;

        PowermoveSpeed = BasePowermoveSpeed;
        canClimb = true;

        anim.ResetTrigger("Jump");
        anim.ResetTrigger("Fall");
        anim.SetBool("Run", false);

        run.Stop();
        grid.Stop();

        bool validLanding =
            (rb.gravityScale > 0f && rb.linearVelocity.y < 0.5f) ||
            (rb.gravityScale < 0f && rb.linearVelocity.y > -0.5f);

        if (validLanding)
        {
            anim.SetTrigger("Land");
            moveSpeed = base_speed;
            burning = false;

            if (land != null)
                land.PlayJuice();
        }
    }

    public bool IsAnimationPlaying(string animationName)
    {
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
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

            spriteRenderer.material = normalMaterial;
        }
    }

    private IEnumerator LowHealthBlink()
    {
        bool toggle = false;

        while (health.current_health == 1)
        {
            spriteRenderer.material = toggle
                ? normalMaterial
                : lowHealthMaterial;

            toggle = !toggle;

            yield return new WaitForSeconds(lowHealthBlinkRate);
        }

        spriteRenderer.material = normalMaterial;
        lowHealthCoroutine = null;
    }

    // =========================================================
    // DAMAGE
    // =========================================================

    public void TakeDamage(int damage, Vector2 ejection_power, Vector3 sourcePosition)
    {
        SC_icecream_eat_system.instance.CancelEat();

        if (damage == 0 && isKnockbackInvincible)
            return;

        if (!canTakeDamage || isInvincible || burning)
            return;

        if (isFrozen || eat_system.isPowerUpActive)
            return;

        Invoke("delay_hit", hitFreezeTime);

        anim.SetBool("Stun", false);
        anim.SetBool("Hit", true);

        if (isClimbing)
            StopClimbingJump();

        if (damage == 0)
        {
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

    private IEnumerator BounceWithKnockback(Vector3 sourcePosition, Vector2 power)
    {
        Vector2 direction = (transform.position - sourcePosition).normalized;

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

    private IEnumerator HitFreezeWithKnockback(Vector3 sourcePosition, Vector2 power)
    {
        isFrozen = true;
        canTakeDamage = false;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        yield return new WaitForSecondsRealtime(hitFreezeTime);

        rb.bodyType = RigidbodyType2D.Dynamic;
        isFrozen = false;

        Vector2 direction = (transform.position - sourcePosition).normalized;

        knockbackVelocity = new Vector2(
            direction.x * hitKnockback.x * power.x,
            hitKnockback.y * transform.localScale.y * power.y
        );

        yield return new WaitForSeconds(0.1f);

        if (!isInvincible)
            canTakeDamage = true;
    }

    // =========================================================
    // LAVA
    // =========================================================

    public void LavaHit(Vector2 launchVelocity, float controlMultiplier, float controlTime)
    {
        burning = true;

        moveSpeed = base_speed;
        PowermoveSpeed = BasePowermoveSpeed;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = launchVelocity;

        StartCoroutine(LavaControlLock(controlMultiplier, controlTime));

        anim.SetBool("Hit", true);
        Invoke("delay", 0.15f);

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
        Invoke("delay_safe", duration + 0.1f);
    }

    public void StartInvincibility(float duration)
    {
        if (invincibilityCoroutine != null)
            StopCoroutine(invincibilityCoroutine);

        invincibilityCoroutine = StartCoroutine(InvincibilityRoutine(duration));

        Invoke("delay", 0.15f);
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
            spriteRenderer.enabled = visible;

            yield return new WaitForSecondsRealtime(0.1f);
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

        if (Mathf.Abs(input.x) > input.y * climbAttachDiagonalLimit)
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

        Invoke("delay_climb", 0.5f);
        climb_delay = false;

        isClimbing = false;
        rb.gravityScale = base_gravity;

        grillage = null;
        canClimb = false;
        rb.linearVelocity = Vector2.zero;

        grid.Stop();
        anim.SetBool("Climb", false);
    }

    private void StopClimbingJump()
    {
        if (!isClimbing)
            return;

        Invoke("delay_climb", 0.5f);

        grillage = null;
        canClimb = false;
        isClimbing = false;
        collision.isTrigger = false;

        climbReattachTimer = climbReattachDelay;

        rb.gravityScale = base_gravity;
        rb.linearVelocity = Vector2.zero;

        grid.Stop();
        anim.SetBool("Climb", false);
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

        if (direction > 0)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
        else if (direction < 0)
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

        isFrozen = false;
        isStunned = false;
        isInvincible = false;
        burning = false;
        canTakeDamage = true;

        moveSpeed = base_speed;
        PowermoveSpeed = BasePowermoveSpeed;

        canClimb = false;
        isClimbing = false;
        grillage = null;

        climbReattachTimer = 0f;
        isDroppingThrough = false;
        dropThroughCoroutine = null;

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
    }

    // =========================================================
    // SCREEN WRAP
    // =========================================================

    private void LateUpdate()
    {
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
