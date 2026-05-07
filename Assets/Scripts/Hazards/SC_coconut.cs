using UnityEngine;
using System.Collections;

public class SC_coconut : MonoBehaviour
{
    [Header("Layer qui déclenche la chute")]
    public LayerMask triggerLayer;

    [Header("Distance de détection")]
    public float rayDistance = 2f;

    [Header("Rebond aléatoire")]
    public float bounceForce = 5f;
    public float randomHorizontalForce = 3f;

    [Header("Flicker")]
    public float flickerDuration = 2f;
    public float flickerInterval = 0.1f;

    [Header("Respawn")]
    public float respawnTime = 5f;

    private Rigidbody2D rb;
    public GameObject coll;
    public SpriteRenderer sr;

    private bool hasFallen = false;
    private bool hasHitGround = false;

    private Vector3 startPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        startPosition = transform.position;

        rb.gravityScale = 0f;
        coll.SetActive(false) ;
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

        coll.SetActive(true);
        rb.gravityScale = 1f;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHitGround) return;

        if (((1 << collision.gameObject.layer) & triggerLayer) != 0
            || collision.CompareTag("Ground")|| collision.CompareTag("Enemy"))
        {
            hasHitGround = true;

            // rebond aléatoire
            float randomX = Random.Range(
                -randomHorizontalForce,
                randomHorizontalForce
            );

            float randomY = Random.Range(
                bounceForce * 0.7f,
                bounceForce * 1.5f
            );

            rb.linearVelocity = new Vector2(randomX, randomY);

            // désactive collision après impact
            coll.SetActive(false);

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

        sr.enabled = false;

        yield return new WaitForSeconds(respawnTime);

        Respawn();
    }

    void Respawn()
    {
        rb.linearVelocity = Vector2.zero;
        transform.position = startPosition;

        ResetCoconut();
    }

    void ResetCoconut()
    {
        hasFallen = false;
        hasHitGround = false;

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        sr.enabled = true;
        coll.SetActive(false);
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