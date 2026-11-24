
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SC_balloon_player : MonoBehaviour
{
    [Header("References")]
    public SC_player player;
    public GameObject balloonVisual;
    public Animator anim;
    public Transform ghost;
    public SC_juiciness bounce;

    [Header("Screen Wrap")]
    private Vector2 limit;

    [Header("Balloon Movement")]
    public float riseSpeed = 4f;
    public float horizontalSpeed = 5f;
    public float riseDelay = 0.5f;

    [Header("Bounce")]
    [Tooltip("Vitesse initiale du rebond")]
    public float bounceForce = 12f;

    [Tooltip("Durée maximale du rebond")]
    public float bounceDuration = 0.6f;

    [Tooltip("Vitesse de décélération du rebond")]
    public float bounceDeceleration = 18f;

    [Tooltip("Délai entre deux rebonds")]
    public float bounceCooldown = 0.15f;

    [Header("Balloon Duration")]
    public float balloonDuration = 5f;

    [Header("Ground Detection")]
    public LayerMask groundLayer;

    [Header("Ceiling Detection")]
    public Transform ceilingCheck;
    public float ceilingCheckDistance = 0.15f;
    public LayerMask ceilingLayer;

    [Header("Deflate")]
    public float deflateDelay = 0.5f;

    public bool isActive;
    private bool isDeflating;
    private bool canRise;
    private bool isBouncing;

    private Rigidbody2D rb;

    private Coroutine riseCoroutine;
    private Coroutine deflateCoroutine;
    private Coroutine durationCoroutine;

    private float lastBounceTime = float.NegativeInfinity;
    private float bounceTimer;

    // Vitesse vectorielle du rebond
    private Vector2 bounceVelocity;

    public InputActionReference move;
    public Vector2 moveInput;

    public static SC_balloon_player instance;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        instance = this;

        rb = player.rb;

        if (balloonVisual != null)
            balloonVisual.SetActive(false);
    }

    // =========================================================
    // ENABLE / DISABLE
    // =========================================================

    private void OnEnable()
    {
        if (move != null)
            move.action.Enable();
    }

    private void OnDisable()
    {
        if (move != null)
            move.action.Disable();
    }

    // =========================================================
    // SCREEN WRAP
    // =========================================================

    private void LateUpdate()
    {
        if (!isActive || player == null)
            return;

        limit = SC_level_master.instance.limits;

        float x = player.transform.position.x;

        if (ghost != null)
        {
            if (x > limit.y - 0.2f)
            {
                ghost.gameObject.SetActive(true);
                ghost.localScale = player.transform.localScale;

                float distance = limit.y - x;
                float ghostX = limit.x - distance;

                ghost.position = new Vector3(
                    ghostX,
                    player.transform.position.y,
                    player.transform.position.z
                );
            }
            else if (x < limit.x + 0.2f)
            {
                ghost.gameObject.SetActive(true);
                ghost.localScale = player.transform.localScale;

                float distance = x - limit.x;
                float ghostX = limit.y + distance;

                ghost.position = new Vector3(
                    ghostX,
                    player.transform.position.y,
                    player.transform.position.z
                );
            }
            else
            {
                ghost.gameObject.SetActive(false);
            }
        }

        if (x > limit.y)
        {
            player.transform.position = new Vector3(
                limit.x,
                player.transform.position.y,
                player.transform.position.z
            );
        }
        else if (x < limit.x)
        {
            player.transform.position = new Vector3(
                limit.y,
                player.transform.position.y,
                player.transform.position.z
            );
        }
    }

    // =========================================================
    // ACTIVATE
    // =========================================================

    public void Activate()
    {
        if (player.eat_system.isPowerUpActive)
            return;

        if (isActive || isDeflating)
            return;

        if (ghost != null)
            ghost.gameObject.SetActive(true);

        isActive = true;
        canRise = false;

        player.enabled = false;

        player.isFrozen = false;
        player.isStunned = false;

        player.knockbackVelocity = Vector2.zero;
        player.externalVelocity = Vector2.zero;

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        if (player.normal != null)
            player.normal.SetActive(false);

        if (player.transformed != null)
            player.transformed.SetActive(false);

        if (balloonVisual != null)
            balloonVisual.SetActive(true);

        riseCoroutine = StartCoroutine(RiseDelayCoroutine());
        durationCoroutine = StartCoroutine(BalloonDurationCoroutine());
    }

    // =========================================================
    // RISE DELAY
    // =========================================================

    private IEnumerator RiseDelayCoroutine()
    {
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(riseDelay);

        if (!isActive || isDeflating)
            yield break;

        canRise = true;
        riseCoroutine = null;
    }

    // =========================================================
    // BALLOON DURATION
    // =========================================================

    private IEnumerator BalloonDurationCoroutine()
    {
        yield return new WaitForSeconds(balloonDuration);

        if (isActive && !isDeflating)
            StartDeflate();

        durationCoroutine = null;
    }

    // =========================================================
    // UPDATE - INPUT
    // =========================================================

    private void Update()
    {
        if (PlayerPrefs.GetFloat("Cheats") == 0)
        {
            if (Input.GetKeyDown(KeyCode.I))
                Activate();
        }

        if (!isActive || isDeflating)
            return;

        if (move != null)
        {
            Vector2 input = move.action.ReadValue<Vector2>();

            moveInput = new Vector2(
                Mathf.Abs(input.x) > 0.2f
                    ? Mathf.Sign(input.x)
                    : 0f,
                0f
            );
        }
    }

    // =========================================================
    // FIXED UPDATE - PHYSICS
    // =========================================================

    private void FixedUpdate()
    {
        if (!isActive || isDeflating)
            return;

        if (!canRise)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Détection du plafond
        CheckCeiling();

        // Si on rebondit, le rebond prend entièrement
        // le contrôle de la vitesse.
        if (isBouncing)
        {
            HandleBounce();
            return;
        }

        // Sinon, mouvement normal
        HandleMovement();
    }

    // =========================================================
    // NORMAL MOVEMENT
    // =========================================================

    private void HandleMovement()
    {
        float horizontalVelocity =
            moveInput.x * horizontalSpeed;

        rb.linearVelocity = new Vector2(
            horizontalVelocity,
            riseSpeed
        );

        UpdateOrientation();
    }

    // =========================================================
    // ORIENTATION
    // =========================================================

    private void UpdateOrientation()
    {
        if (moveInput.x > 0)
        {
            Vector3 scale = player.transform.localScale;

            player.transform.localScale = new Vector3(
                Mathf.Abs(scale.x),
                scale.y,
                scale.z
            );
        }
        else if (moveInput.x < 0)
        {
            Vector3 scale = player.transform.localScale;

            player.transform.localScale = new Vector3(
                -Mathf.Abs(scale.x),
                scale.y,
                scale.z
            );
        }
    }

    // =========================================================
    // COLLISIONS
    // =========================================================

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActive || isDeflating || !canRise)
            return;

        if (((1 << collision.gameObject.layer) & groundLayer) == 0)
            return;

        if (Time.time < lastBounceTime + bounceCooldown)
            return;

        Vector2 normalSum = Vector2.zero;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);

            normalSum += contact.normal;
        }

        if (normalSum.sqrMagnitude < 0.001f)
            return;

        Vector2 bounceDirection = normalSum.normalized;

        StartBounce(bounceDirection);
    }
    // =========================================================
    // START BOUNCE
    // =========================================================

    private void StartBounce(Vector2 direction)
    {
        if (isBouncing || isDeflating)
            return;

        if (Time.time < lastBounceTime + bounceCooldown)
            return;

        lastBounceTime = Time.time;

        isBouncing = true;

        bounce.PlayJuice();

        bounceTimer = 0f;

        direction.Normalize();

        bounceVelocity = direction * bounceForce;

        rb.linearVelocity = Vector2.zero;
    }    // =========================================================
    // HANDLE BOUNCE
    // =========================================================

    private void HandleBounce()
    {
        // Applique la vitesse actuelle du rebond
        rb.linearVelocity = bounceVelocity;

        // Décélération uniforme du vecteur complet.
        // La direction reste identique pendant le ralentissement.
        bounceVelocity = Vector2.MoveTowards(
            bounceVelocity,
            Vector2.zero,
            bounceDeceleration * Time.fixedDeltaTime
        );

        bounceTimer += Time.fixedDeltaTime;

        // Fin du rebond
        if (bounceTimer >= bounceDuration ||
            bounceVelocity.sqrMagnitude < 0.01f)
        {
            EndBounce();
        }
    }

    // =========================================================
    // END BOUNCE
    // =========================================================

    private void EndBounce()
    {
        isBouncing = false;

        bounceVelocity = Vector2.zero;

        rb.linearVelocity = Vector2.zero;

        // Le joueur reprend immédiatement le contrôle.
        // Le mouvement normal reprend au prochain FixedUpdate.
    }

    // =========================================================
    // CEILING DETECTION
    // =========================================================

    private void CheckCeiling()
    {
        if (ceilingCheck == null)
            return;

        RaycastHit2D hit = Physics2D.Raycast(
            ceilingCheck.position,
            Vector2.up,
            ceilingCheckDistance,
            ceilingLayer
        );

        Debug.DrawRay(
            ceilingCheck.position,
            Vector2.up * ceilingCheckDistance,
            Color.red
        );

        if (hit.collider != null)
        {
            if (Time.time >= lastBounceTime + bounceCooldown)
            {
                // Utilise la normale réelle du plafond
                StartBounce(hit.normal);
            }
        }
    }

    // =========================================================
    // DAMAGE
    // =========================================================

    public void BalloonTakeDamage()
    {

        Exit();

        player.TakeDamage(
            1,
            Vector2.one,
            transform.position
        );
    }

    // =========================================================
    // DEFLATE
    // =========================================================

    private void StartDeflate()
    {
        if (!isActive || isDeflating)
            return;

        if (ghost != null)
            ghost.gameObject.SetActive(false);

        isDeflating = true;
        canRise = false;
        isBouncing = false;

        bounceVelocity = Vector2.zero;

        if (riseCoroutine != null)
        {
            StopCoroutine(riseCoroutine);
            riseCoroutine = null;
        }

        rb.linearVelocity = Vector2.zero;

        if (anim != null)
            anim.SetTrigger("Deflate");

        if (deflateCoroutine != null)
            StopCoroutine(deflateCoroutine);

        deflateCoroutine = StartCoroutine(DeflateCoroutine());
    }

    // =========================================================
    // DEFLATE COROUTINE
    // =========================================================

    private IEnumerator DeflateCoroutine()
    {
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(deflateDelay);

        Exit();

        deflateCoroutine = null;
    }

    // =========================================================
    // EXIT
    // =========================================================

    public void Exit()
    {
        if (!isActive)
            return;

        if (ghost != null)
            ghost.gameObject.SetActive(false);

        isActive = false;
        canRise = false;
        isDeflating = false;
        isBouncing = false;

        bounceVelocity = Vector2.zero;

        if (riseCoroutine != null)
            StopCoroutine(riseCoroutine);

        if (deflateCoroutine != null)
            StopCoroutine(deflateCoroutine);

        if (durationCoroutine != null)
            StopCoroutine(durationCoroutine);

        riseCoroutine = null;
        deflateCoroutine = null;
        durationCoroutine = null;

        player.enabled = true;

        rb.gravityScale = player.base_gravity;
        rb.linearVelocity = Vector2.zero;

        if (balloonVisual != null)
            balloonVisual.SetActive(false);

        if (player.normal != null)
            player.normal.SetActive(true);
    }
}
