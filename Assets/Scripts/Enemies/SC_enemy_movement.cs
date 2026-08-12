using UnityEngine;

public class SC_enemy_movement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public Transform groundDetection;
    public float groundDistance = 0.8f;
    public float wallDistance = 0.3f;
    public float flipCooldown = 0.3f;

    [Header("Enemy Detection")]
    public LayerMask enemyLayer;
    public float enemyDetectionDistance = 1f;
    public Vector2 enemyDetectionSize = new Vector2(1f, 1f);

    [Header("Ground")]
    public LayerMask groundLayer;

    [Header("State")]
    public bool CanMove = true;

    [Header("Damage")]
    public SC_enemy_damage damage;

    private bool movingRight = true;
    private float lastFlipTime;

    // Collider de cet ennemi
    public Collider2D ownCollider;




    void Update()
    {
        if (!CanMove)
            return;

        if (damage != null && (damage.isStunned || damage.isKnockedBack))
            return;

        Move();
    }


    // =========================================================
    // MOVEMENT
    // =========================================================

    void Move()
    {
        bool groundAhead = CheckGround();
        bool wallAhead = CheckWall();
        bool enemyAhead = CheckEnemy();

        // Si quelque chose bloque devant :
        // - plus de sol
        // - mur
        // - autre ennemi
        if ((!groundAhead || wallAhead || enemyAhead) &&
            Time.time - lastFlipTime >= flipCooldown)
        {
            Flip();
            return;
        }

        Vector3 direction = movingRight
            ? Vector3.right
            : Vector3.left;

        transform.position += direction * moveSpeed * Time.deltaTime;
    }


    // =========================================================
    // DETECTION DU SOL
    // =========================================================

    bool CheckGround()
    {
        if (groundDetection == null)
            return true;

        RaycastHit2D hit = Physics2D.Raycast(
            groundDetection.position,
            -groundDetection.up,
            groundDistance,
            groundLayer
        );

        return hit.collider != null;
    }


    // =========================================================
    // DETECTION DU MUR
    // =========================================================

    bool CheckWall()
    {
        if (groundDetection == null)
            return false;

        Vector2 direction = movingRight
            ? Vector2.right
            : Vector2.left;

        RaycastHit2D hit = Physics2D.Raycast(
            groundDetection.position,
            direction,
            wallDistance,
            groundLayer
        );

        return hit.collider != null;
    }


    // =========================================================
    // DETECTION D'UN AUTRE ENNEMI
    // =========================================================

    bool CheckEnemy()
    {
        Vector2 direction = movingRight
            ? Vector2.right
            : Vector2.left;

        // Centre de la zone de détection
        Vector2 detectionPosition =
            (Vector2)transform.position +
            direction * (enemyDetectionDistance / 2f);

        // Cherche tous les colliders Enemy
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            detectionPosition,
            enemyDetectionSize,
            0f,
            enemyLayer
        );

        foreach (Collider2D hit in hits)
        {
            // Ignore uniquement notre propre collider
            if (hit == ownCollider)
                continue;

            // Un autre collider appartenant à notre propre objet
            // est également ignoré
            if (hit.transform == transform)
                continue;

            // Un autre ennemi a été trouvé
            return true;
        }

        return false;
    }


    // =========================================================
    // CHANGEMENT DE DIRECTION
    // =========================================================

    public void Flip()
    {
        movingRight = !movingRight;

        Vector3 scale = transform.localScale;

        scale.x *= -1;

        transform.localScale = scale;

        lastFlipTime = Time.time;
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmos()
    {
        // -------------------------
        // SOL
        // -------------------------

        if (groundDetection != null)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawRay(
                groundDetection.position,
                -groundDetection.up * groundDistance
            );
        }


        // -------------------------
        // MUR
        // -------------------------

        if (groundDetection != null)
        {
            Gizmos.color = Color.red;

            Vector3 wallDirection = movingRight
                ? Vector3.right
                : Vector3.left;

            Gizmos.DrawRay(
                groundDetection.position,
                wallDirection * wallDistance
            );
        }


        // -------------------------
        // ENNEMI
        // -------------------------

        Gizmos.color = Color.blue;

        Vector3 enemyDirection = movingRight
            ? Vector3.right
            : Vector3.left;

        Vector3 detectionPosition =
            transform.position +
            enemyDirection * (enemyDetectionDistance / 2f);

        Gizmos.DrawWireCube(
            detectionPosition,
            enemyDetectionSize
        );
    }
}