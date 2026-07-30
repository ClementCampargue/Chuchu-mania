using UnityEngine;

public class SC_enemy_movement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public Transform groundDetection;
    public float groundDistance = 0.8f;
    public float wallDistance = 0.3f;
    public float flipCooldown = 0.3f;

    [Header("Layers")]
    public LayerMask groundLayer;
    public LayerMask enemyLayer;
    public float enemyDetectionDistance = 0.5f;

    [Header("State")]
    public bool CanMove = true;

    public SC_enemy_damage damage;

    private bool movingRight = true;
    private float lastFlipTime;


    void Update()
    {
        if (!CanMove) return;

        if (damage != null && (damage.isStunned || damage.isKnockedBack))
            return;

        Move();
    }


    void Move()
    {
        bool groundAhead = CheckGround();
        bool wallAhead = CheckWall();
        bool enemyAhead = CheckEnemy();

        // On tourne avant d'avancer
        if ((!groundAhead || wallAhead || enemyAhead) &&
            Time.time - lastFlipTime > flipCooldown)
        {
            Flip();
            return;
        }


        Vector3 direction = movingRight ? Vector3.right : Vector3.left;

        transform.position += direction * moveSpeed * Time.deltaTime;
    }


    bool CheckGround()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            groundDetection.position,
            -groundDetection.up,
            groundDistance,
            groundLayer
        );

        return hit.collider != null;
    }


    bool CheckWall()
    {
        Vector2 direction = movingRight ? Vector2.right : Vector2.left;

        RaycastHit2D hit = Physics2D.Raycast(
            groundDetection.position,
            direction,
            wallDistance,
            groundLayer
        );

        return hit.collider != null;
    }


    bool CheckEnemy()
    {
        Vector2 direction = movingRight ? Vector2.right : Vector2.left;

        RaycastHit2D hit = Physics2D.Raycast(
            groundDetection.position,
            direction,
            enemyDetectionDistance,
            enemyLayer
        );

        return hit.collider != null && hit.collider.gameObject != gameObject;
    }


    public void Flip()
    {
        movingRight = !movingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;

        lastFlipTime = Time.time;
    }


    private void OnDrawGizmos()
    {
        if (groundDetection == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawRay(
            groundDetection.position,
                        -groundDetection.up *groundDistance
        );

        Gizmos.color = Color.red;
        Gizmos.DrawRay(
            groundDetection.position,
            (movingRight ? Vector2.right : Vector2.left) * wallDistance
        );

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(
            groundDetection.position,
            (movingRight ? Vector2.right : Vector2.left) * enemyDetectionDistance
        );
    }
}