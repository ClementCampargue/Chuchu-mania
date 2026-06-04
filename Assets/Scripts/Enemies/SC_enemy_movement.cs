using UnityEngine;
using System.Collections;

public class SC_enemy_movement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public Transform groundDetection;
    public float detectionDistance = 1f;
    public float flipCooldown = 0.2f;

    [Header("Layers")]
    public LayerMask groundLayer;
    public LayerMask enemyLayer;
    public float enemyDetectionDistance = 0.2f;

    private bool movingRight = true;
    private float lastFlipTime;

    public bool CanMove = true;
    public SC_enemy_damage damgage;
    void Update()
    {
        if (!CanMove || damgage.isStunned|| damgage.isKnockedBack) return;

        HandleMovement();
    }

    void HandleMovement()
    {
        transform.position += (movingRight ? Vector3.right : Vector3.left)
                               * moveSpeed
                               * Time.deltaTime;

        bool isGroundAhead =
            Physics2D.Raycast(groundDetection.position, Vector2.down, detectionDistance, groundLayer);

        bool isWallAhead =
            Physics2D.Raycast(groundDetection.position,
                               movingRight ? Vector2.right : Vector2.left,
                               0.1f,
                               groundLayer);

        RaycastHit2D enemyHit =
            Physics2D.Raycast(groundDetection.position,
                              movingRight ? Vector2.right : Vector2.left,
                              enemyDetectionDistance,
                              enemyLayer);

        bool isEnemyAhead = enemyHit.collider != null
                            && enemyHit.collider.gameObject != gameObject;

        if ((!isGroundAhead || isWallAhead || isEnemyAhead)
            && Time.time - lastFlipTime > flipCooldown)
        {
            Flip();
            lastFlipTime = Time.time;
        }
    }

    public void Flip()
    {
        movingRight = !movingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}