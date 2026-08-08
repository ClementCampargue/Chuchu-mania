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
    public bool canJump = true;
    [Header("Climbing")]
    public float climbSpeed = 4f;
    private bool justLanded;
    private bool isClimbing;
    private bool canClimb;
    private SC_grillage grillage;
    private sc_health_system health;
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

    public SC_icecream_eat_system eat_system;
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

    void Start()
    {
        canTakeDamage = true;
        health = sc_health_system.instance;
        normal.SetActive(true);
        transformed.SetActive(false);
        limit = SC_game_master.instance.limits;
        base_gravity = rb.gravityScale;
        spriteRenderer.material = normalMaterial;
        anim = anim_;
    }

    void Update()
    {
        limit = SC_level_master.instance.limits;

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

        if (!isFrozen  && canMove && Time.timeScale != 0)
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

        if (!wasGrounded && isGrounded)
        {
            anim.ResetTrigger("Jump");



            if (!wasGrounded && isGrounded && rb.linearVelocity.y <= 0.5f)
            {
                anim.ResetTrigger("Jump");
                anim.SetTrigger("Land");
                land.PlayJuice();
            }
            else if (transform.localScale.y == -1 && rb.linearVelocity.y >= -0.5f)

            {
                anim.ResetTrigger("Jump");
                anim.SetTrigger("Land");
                land.PlayJuice();
            }

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
        if (eat_system.isPowerUpActive) return;

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
        if (eat_system.isPowerUpActive) return;

        if (isInvincible) return;
        StartCoroutine(StunCoroutine());
    }
    void CheckDamage()
    {
        if (!canTakeDamage || health.current_health == 0) return;

        hit = Physics2D.OverlapCircle(damageCheck.position, damageRadius, damageLayer);
        if (hit != null)
        {
            TakeDamage(1, hit.transform.position);
            if(health.current_health == 0)
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
        if (isStunned || isInvincible || eat_system.isPowerUpActive)
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
    IEnumerator LowHealthBlink()
    {
        bool toggle = false;

        while (health.current_health == 1)
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
        if (!canMove) return;
        if (Time.timeScale ==0) return;
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
        if (Time.timeScale == 0) return;

        if (!canJump) return;

        if (isStunned) return;
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

        // --- Ghost pour le screen wrap ---
        if (x > (limit - levelWidth / 2))
        {
            ghost.gameObject.SetActive(true);
            ghost.position = new Vector3(
                x - levelWidth,
                transform.position.y,
                transform.position.z
            );
        }
        else if (x < (-limit + levelWidth / 2))
        {
            ghost.gameObject.SetActive(true);
            ghost.position = new Vector3(
                x + levelWidth,
                transform.position.y,
                transform.position.z
            );
        }
        else
        {
            ghost.gameObject.SetActive(false);
        }

        // --- Screen wrap ---
        if (x > limit)
        {
            transform.position = new Vector3(
                -limit,
                transform.position.y,
                transform.position.z
            );

            // Repart vers la gauche
            if (rb.linearVelocity.x > 0)
            {
                rb.linearVelocity = new Vector2(
                    -Mathf.Abs(rb.linearVelocity.x),
                    rb.linearVelocity.y
                );
            }
        }
        else if (x < -limit)
        {
            transform.position = new Vector3(
                limit,
                transform.position.y,
                transform.position.z
            );

            // Repart vers la droite
            if (rb.linearVelocity.x < 0)
            {
                rb.linearVelocity = new Vector2(
                    Mathf.Abs(rb.linearVelocity.x),
                    rb.linearVelocity.y
                );
            }
        }
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

        isStunned = false;
        ps_damage.Play();
        health.take_damage(damage);
        eat_system.take_damage();

        if (health.current_health > 0)
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
        rb.bodyType = RigidbodyType2D.Dynamic;

        rb.linearVelocity = launchVelocity;

        StartCoroutine(LavaControlLock(controlMultiplier, controlTime));


        if (!canTakeDamage) return;
        if (isFrozen || isInvincible || eat_system.isPowerUpActive) return;


        anim.SetBool("Stun", false);
        isStunned = false;
        ps_damage.Play();

        isFrozen = false;
        isStunned = false;
        canTakeDamage = true;



        eat_system.take_damage();
        health.take_damage(1);


        if (health.current_health > 0)
        {
            if (hitCoroutine != null) StopCoroutine(hitCoroutine);
            StartCoroutine(InvincibilityCoroutine());
        }
        else
        {
            Die();
        }
        damage_lava_sfx.PlayJuice();
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
        knockbackVelocity = new Vector2(direction.x * hitKnockback.x, hitKnockback.y * transform.localScale.y);

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
        canMove = false;
        if (hitCoroutine != null) StopCoroutine(hitCoroutine);
        GetComponent<SortingGroup>().sortingLayerName = "UI";
        isFrozen = true;
        anim.SetBool("Die",true);
        anim.SetBool("Eat", false);
        eat_system.eating_sfx.Stop();
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
    public void FaceTarget(Transform target)
    {
        if (target == null) return;

        float direction = target.position.x - transform.position.x;

        if (direction > 0)
        {
            // Le PNJ est à droite
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
        else if (direction < 0)
        {
            // Le PNJ est à gauche
            transform.localScale = new Vector3(
                -Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
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
        normal.SetActive(true);
        transformed.SetActive(false);

        StopAllCoroutines();
        isFrozen = false;
        isStunned = false;
        isInvincible = false;
        canTakeDamage = true;
        anim_.SetBool("Die", false);
        if (health != null)
        {
            health.revive();
        }


        collider.enabled = true;
        spriteRenderer.enabled = true;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.angularVelocity = 0;
        eat_system.ResetSystem();
    }

}