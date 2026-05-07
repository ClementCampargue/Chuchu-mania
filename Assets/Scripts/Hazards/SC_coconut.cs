using UnityEngine;
using System.Collections;

public class SC_coconut : MonoBehaviour
{
    [Header("Layer qui déclenche la chute")]
    public LayerMask triggerLayer;

    [Header("Distance de détection")]
    public float rayDistance = 2f;

    [Header("Rebond")]
    public float bounceForce = 5f;

    [Header("Flicker")]
    public float flickerDuration = 2f;
    public float flickerInterval = 0.1f;

    private Rigidbody2D rb;
    private CircleCollider2D col;
    public SpriteRenderer sr;

    private bool hasFallen = false;
    private bool hasHitGround = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();

        rb.gravityScale = 0f;
        col.enabled = false;
    }

    void Update()
    {
        if (hasFallen) return;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            rayDistance,
            triggerLayer
        );

        if (hit.collider != null)
        {
            Fall();
        }
    }

    void Fall()
    {
        hasFallen = true;

        col.enabled = true;
        rb.gravityScale = 1f;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHitGround) return;

        if (((1 << collision.gameObject.layer) & triggerLayer) != 0 ||collision.tag == "Ground")
        {
            hasHitGround = true;

            // rebond
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);

            // désactive collision après impact pour éviter bug physique
            col.enabled = false;

            StartCoroutine(FlickerThenDestroy());
        }
    }


    IEnumerator FlickerThenDestroy()
    {
        float elapsed = 0f;

        while (elapsed < flickerDuration)
        {
            sr.enabled = !sr.enabled;

            yield return new WaitForSeconds(flickerInterval);

            elapsed += flickerInterval;
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            transform.position,
            transform.position + Vector3.down * rayDistance
        );
    }
}