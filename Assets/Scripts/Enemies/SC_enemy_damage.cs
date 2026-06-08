using System.Collections;
using UnityEngine;

public class SC_enemy_damage : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public ParticleSystem ps;
    public Transform visuals;

    [Header("Collision")]
    public Transform collision;
    public Transform stunDetection;

    [Header("Layers")]
    public LayerMask playerLayer;
    public LayerMask stunLayer;

    [Header("Detection")]
    public float detectionRadius = 0.5f;
    public float stunRadius = 0.5f;

    [Header("Knockback")]
    public float knockbackForce = 5f;
    public float knockbackVerticalForce = 5f;
    public float spinSpeed = 360f;
    public float gravity = 9.8f;

    [Header("Stun")]
    public float stunDuration = 2f;

    [Header("Flicker")]
    public float flickerDuration = 2f;
    public float flickerInterval = 0.1f;

    [Header("Juice")]
    public SC_juiciness juice;
    public SC_juiciness juice_stun;

    private Vector2 knockbackVelocity;
    public bool isKnockedBack;
    public bool isStunned;

    private SC_icecream_eat_system eat;

    private SC_player player;
    public bool die_burn;
    void Start()
    {
        eat = SC_icecream_eat_system.instance;
        player = SC_player.instance;
    }

    void Update()
    {

        DetectPlayerOverlap();
        DetectStun();

        if (isKnockedBack)
            HandleKnockback();
    }

    void DetectPlayerOverlap()
    {
        if (isKnockedBack || isStunned) return;
        Collider2D playerCollider =
            Physics2D.OverlapCircle(collision.position, detectionRadius, playerLayer);


        if (playerCollider != null && die_burn && player.burning)
        {
            bool playerOnRight = playerCollider.transform.position.x > transform.position.x;
            Knockback(playerOnRight);
        }
        if (playerCollider != null && eat.isPowerUpActive)
        {
            bool playerOnRight = playerCollider.transform.position.x > transform.position.x;
            Knockback(playerOnRight);
        }
    }

    void DetectStun()
    {
        if (isStunned || isKnockedBack) return;

        Collider2D stunCollider =
            Physics2D.OverlapCircle(stunDetection.position, stunRadius, stunLayer);

        if (stunCollider != null)
            StartCoroutine(Stun());
    }

    IEnumerator Stun()
    {
        if (isStunned) yield break;

        isStunned = true;

        if (animator != null)
        {
            animator.SetBool("Stun", true);
        }
        juice_stun.PlayJuice();

        yield return new WaitForSeconds(stunDuration);

        if (animator != null)
        {
            animator.SetBool("Stun", false);
        }

        isStunned = false;

    }

    public void Knockback(bool playerOnRight)
    {
        if (isKnockedBack) return;

        isKnockedBack = true;
        player.anim_powerup.SetTrigger("Punch");
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }
        if (ps != null)
        {
            ps.Play();
        }
        juice.PlayJuice();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        float dir = playerOnRight ? -1f : 1f;

        knockbackVelocity = new Vector2(
            dir * knockbackForce,
            knockbackVerticalForce
        );

        StartCoroutine(FlickerAndDestroy());
    }

    void HandleKnockback()
    {
        knockbackVelocity.y -= gravity * Time.deltaTime;

        transform.position += (Vector3)(knockbackVelocity * Time.deltaTime);

        if (visuals != null)
            visuals.Rotate(Vector3.forward * spinSpeed * Time.deltaTime);
    }

    IEnumerator FlickerAndDestroy()
    {
        float t = 0f;

        while (t < flickerDuration)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;

            yield return new WaitForSeconds(flickerInterval);
            t += flickerInterval;
        }

        Destroy(gameObject);
    }
}