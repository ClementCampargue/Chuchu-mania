using UnityEngine;

[RequireComponent(typeof(SC_guard_tracker))]
public class SC_guard_movement : MonoBehaviour
{
    public enum MovementMode
    {
        Normal,
        Panic
    }

    [Header("Mode")]
    public MovementMode movementMode = MovementMode.Normal;

    [Header("Déplacement")]
    public float moveSpeed = 3f;
    public float acceleration = 12f;
    public float rotationSpeed = 720f;

    [Header("Nodes")]
    public float nodeReachDistance = 0.25f;

    [Header("Panique / Topi Taupe")]
    [Tooltip("Le garde dépasse légèrement le point avant de changer de direction.")]
    public float panicOvershoot = 0.5f;

    [Tooltip("Vitesse minimale pendant un changement de direction.")]
    public float panicMinSpeed = 1.5f;

    [Tooltip("Variation aléatoire de vitesse.")]
    public float panicSpeedVariation = 1f;

    [Tooltip("Temps minimum avant de pouvoir changer de direction.")]
    public float panicDirectionChangeDelay = 0.15f;

    [Tooltip("Si activé, le garde peut repartir vers le node précédent.")]
    public bool allowReverseInPanic = true;

    private SC_guard_tracker tracker;

    private Vector3 velocity;

    private float currentSpeed;

    private float directionChangeTimer;

    private int lastDirection = 1;

    private void Awake()
    {
        tracker = GetComponent<SC_guard_tracker>();

        currentSpeed = moveSpeed;
    }

    private void Update()
    {
        if (tracker == null || !tracker.HasPath())
            return;

        directionChangeTimer -= Time.deltaTime;

        switch (movementMode)
        {
            case MovementMode.Normal:
                MoveNormal();
                break;

            case MovementMode.Panic:
                MovePanic();
                break;
        }
    }

    // =========================================================
    // MODE NORMAL
    // =========================================================

    private void MoveNormal()
    {
        Vector3 target =
            tracker.GetCurrentDestination();

        MoveTowards(target, moveSpeed);

        if (Vector3.Distance(
                transform.position,
                target
            ) <= nodeReachDistance)
        {
            tracker.AdvanceToNextPoint();
        }
    }

    // =========================================================
    // MODE PANIQUE
    // =========================================================

    private void MovePanic()
    {
        Vector3 target =
            tracker.GetCurrentDestination();

        Vector3 direction =
            target - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            direction.Normalize();

            float randomSpeed =
                moveSpeed +
                Random.Range(
                    -panicSpeedVariation,
                    panicSpeedVariation
                );

            randomSpeed =
                Mathf.Max(
                    panicMinSpeed,
                    randomSpeed
                );

            MoveTowards(
                transform.position + direction,
                randomSpeed
            );
        }

        float distance =
            Vector3.Distance(
                transform.position,
                target
            );

        if (distance <= nodeReachDistance + panicOvershoot)
        {
            tracker.AdvanceToNextPoint();

            if (allowReverseInPanic &&
                directionChangeTimer <= 0f)
            {
                directionChangeTimer =
                    panicDirectionChangeDelay;

                lastDirection *= -1;
            }
        }
    }

    // =========================================================
    // MOVEMENT
    // =========================================================

    private void MoveTowards(
        Vector3 target,
        float targetSpeed)
    {
        Vector3 direction =
            target - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();

        currentSpeed =
            Mathf.MoveTowards(
                currentSpeed,
                targetSpeed,
                acceleration * Time.deltaTime
            );

        velocity =
            Vector3.Lerp(
                velocity,
                direction * currentSpeed,
                acceleration * Time.deltaTime
            );

        transform.position +=
            velocity * Time.deltaTime;

        RotateTowards(direction);
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
    }
}