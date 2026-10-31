using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SC_balloon_player : MonoBehaviour
{
    [Header("References")]
    public SC_player player;

    [Tooltip("GameObject visuel du ballon")]
    public GameObject balloonVisual;
    [Header("Screen Wrap")]
    private Vector2 limit;

    [Header("Balloon Movement")]
    public float riseSpeed = 4f;
    public float horizontalSpeed = 5f;

    [Tooltip("Délai avant que le ballon commence à monter")]
    public float riseDelay = 0.5f;

    [Header("Deflate")]
    [Tooltip("Délai après avoir touché le sol avant de redevenir normal")]
    public float deflateDelay = 0.5f;
    [Header("Ceiling Detection")]
    public Transform ceilingCheck;
    public float ceilingCheckDistance = 0.15f;
    public LayerMask ceilingLayer;
    private bool isActive;
    private bool isDeflating;
    private bool canRise;

    private Rigidbody2D rb;

    private Coroutine riseCoroutine;
    private Coroutine deflateCoroutine;

    public InputActionReference move;

    public Vector2 moveInput;

    public static SC_balloon_player instance;

    public Animator anim;
    public Transform ghost;

    // =========================================================
    // AWAKE
    // =========================================================

    private void LateUpdate()
    {
        if (!isActive || player == null)
            return;
        limit = SC_level_master.instance.limits;

        // Récupère les limites du niveau
        limit = SC_level_master.instance.limits;

        float x = player.transform.position.x;

        // Affichage du ghost près des bords
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

        // Passage d'un bord à l'autre
        if (x > limit.y)
        {
            player.transform.position = new Vector3(
                limit.x,
                player.transform.position.y,
                player.transform.position.z
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
            player.transform.position = new Vector3(
                limit.y,
                player.transform.position.y,
                player.transform.position.z
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
    // ACTIVATE
    // =========================================================

    public void Activate()
    {
        if (player.eat_system.isPowerUpActive)
            return;

        if (isActive)
            return;

        if (isDeflating)
            return;
        ghost.gameObject.SetActive(true);

        isActive = true;
        canRise = false;

        // Désactive les mouvements normaux
        player.enabled = false;
        player.canMove = false;

        player.isFrozen = false;
        player.isStunned = false;

        // Stop mouvements précédents
        player.knockbackVelocity = Vector2.zero;
        player.externalVelocity = Vector2.zero;

        rb.linearVelocity = Vector2.zero;

        // Pas de gravité :
        // le ballon contrôlera lui-même sa vitesse verticale
        rb.gravityScale = 0f;

        // Désactive le joueur normal
        if (player.normal != null)
            player.normal.SetActive(false);

        if (player.transformed != null)
            player.transformed.SetActive(false);

        // Active le ballon
        if (balloonVisual != null)
            balloonVisual.SetActive(true);

        // Lance le délai avant la montée
        if (riseCoroutine != null)
            StopCoroutine(riseCoroutine);

        riseCoroutine = StartCoroutine(RiseDelayCoroutine());
    }

    // =========================================================
    // RISE DELAY
    // =========================================================

    private IEnumerator RiseDelayCoroutine()
    {
        // Le ballon reste complètement immobile
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(riseDelay);

        // Vérifie que le ballon est toujours actif
        if (!isActive || isDeflating)
        {
            riseCoroutine = null;
            yield break;
        }

        canRise = true;

        riseCoroutine = null;
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (PlayerPrefs.GetFloat("Cheats") == 0)
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                Activate();
            }
        }


        if (player.eat_system.isPowerUpActive)
            return;
        if (!isActive)
            return;

        if (Time.timeScale == 0)
            return;

        // Si le ballon est en train de se dégonfler,
        // il ne peut plus bouger
        if (isDeflating)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Pendant le délai de départ
        if (!canRise)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        CheckCeiling();
        HandleMovement();
    }

    // =========================================================
    // MOVEMENT
    // =========================================================

    private void HandleMovement()
    {
        Vector2 input = Vector2.zero;

        if (move != null)
            input = move.action.ReadValue<Vector2>();

        // On conserve uniquement gauche / droite
        moveInput = new Vector2(
            Mathf.Abs(input.x) > 0.2f
                ? Mathf.Sign(input.x)
                : 0f,
            0f
        );

        // Le ballon monte toujours
        float verticalVelocity = riseSpeed;

        // Le joueur contrôle uniquement X
        float horizontalVelocity =
            moveInput.x * horizontalSpeed;

        rb.linearVelocity = new Vector2(
            horizontalVelocity,
            verticalVelocity
        );

        // =====================================================
        // ORIENTATION
        // =====================================================

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

    private void CheckCeiling()
    {
        if (ceilingCheck == null)
            return;
        Debug.DrawRay(
    ceilingCheck.position,
    Vector2.up * ceilingCheckDistance,
    Color.red
);
        RaycastHit2D hit = Physics2D.Raycast(
            ceilingCheck.position,
            Vector2.up,
            ceilingCheckDistance,
            ceilingLayer
        );

        if (hit.collider != null)
        {
            StartDeflate();
        }
    }
    // =========================================================
    // DAMAGE
    // =========================================================

    private void BalloonTakeDamage()
    {
        if (player.eat_system.isPowerUpActive)
            return;
        if (!isActive)
            return;

        // On quitte immédiatement le mode ballon
        Exit();

        // Le joueur reprend son système de dégâts normal
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
        if (!isActive)
            return;

        if (isDeflating)
            return;

        ghost.gameObject.SetActive(false);
        isDeflating = true;
        canRise = false;

        // Stoppe le délai de montée s'il est encore en cours
        if (riseCoroutine != null)
        {
            StopCoroutine(riseCoroutine);
            riseCoroutine = null;
        }

        // Le ballon ne bouge plus
        rb.linearVelocity = Vector2.zero;

        // Animation de dégonflage
        if (anim != null)
            anim.SetTrigger("Deflate");

        // Lance le délai avant retour au joueur normal
        if (deflateCoroutine != null)
            StopCoroutine(deflateCoroutine);

        deflateCoroutine =
            StartCoroutine(DeflateCoroutine());
    }

    // =========================================================
    // DEFLATE COROUTINE
    // =========================================================

    private IEnumerator DeflateCoroutine()
    {
        // Le ballon reste au sol pendant tout le délai
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(deflateDelay);

        // Retour au joueur normal
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

        ghost.gameObject.SetActive(false);
        isActive = false;
        canRise = false;
        isDeflating = false;

        // Stoppe les coroutines
        if (riseCoroutine != null)
        {
            StopCoroutine(riseCoroutine);
            riseCoroutine = null;
        }

        if (deflateCoroutine != null)
        {
            StopCoroutine(deflateCoroutine);
            deflateCoroutine = null;
        }

        // Réactive le joueur
        player.enabled = true;

        // Remet la physique normale
        rb.gravityScale = player.base_gravity;
        rb.linearVelocity = Vector2.zero;

        // Réactive les mouvements normaux
        player.canMove = true;

        // Désactive le ballon
        if (balloonVisual != null)
            balloonVisual.SetActive(false);

        // Réactive le joueur normal
        if (player.normal != null)
            player.normal.SetActive(true);
    }
}
