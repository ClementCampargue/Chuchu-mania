using UnityEngine;

public class SC_spawn_velocity : MonoBehaviour
{
    [Header("Velocity")]
    [SerializeField] private Vector2 velocity = new Vector2(5f, 0f);

    [Header("Orientation")]
    [SerializeField] private bool orientWithVelocity = true;
    [SerializeField] private bool forward_velocity = true;
    [SerializeField] private bool orientVersJoueur = false;

    [Header("Directional Snap")]
    [SerializeField] private bool snapDirection = false;
    [SerializeField] private int snapDirections = 8;

    private Rigidbody2D rb;
    private SC_player player;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        player = SC_player.instance;

        // Oriente la vélocité vers le joueur au spawn
        if (orientVersJoueur)
        {
            if (player != null)
            {
                Vector2 direction = (player.transform.position - transform.position).normalized;

                if (snapDirection)
                {
                    direction = SnapDirection(direction);
                }

                rb.linearVelocity = direction * velocity.magnitude;

                if (orientWithVelocity)
                {
                    SetRotationFromDirection(direction);
                }

                return;
            }
        }

        // Comportement normal
        if (forward_velocity)
        {
            Vector2 direction = transform.right;

            if (snapDirection)
            {
                direction = SnapDirection(direction);
            }

            rb.linearVelocity = direction * velocity.x;

            if (orientWithVelocity)
            {
                SetRotationFromDirection(direction);
            }
        }
        else
        {
            Vector2 direction = velocity;

            if (direction.sqrMagnitude > 0f)
            {
                if (snapDirection)
                {
                    direction = SnapDirection(direction);
                }

                rb.linearVelocity = direction;

                if (orientWithVelocity)
                {
                    SetRotationFromDirection(direction);
                }
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    private Vector2 SnapDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0f)
            return direction;

        snapDirections = Mathf.Max(1, snapDirections);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        float step = 360f / snapDirections;
        float snappedAngle = Mathf.Round(angle / step) * step;

        float radians = snappedAngle * Mathf.Deg2Rad;

        return new Vector2(
            Mathf.Cos(radians),
            Mathf.Sin(radians)
        ).normalized;
    }

    private void SetRotationFromDirection(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}