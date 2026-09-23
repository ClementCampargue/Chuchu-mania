using UnityEngine;

public class SC_grillage : MonoBehaviour
{
    [Header("Limits")]
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;


    // =========================================================
    // CLIMBING
    // =========================================================

    [Header("Climbing")]

    [Tooltip("Si activé, le joueur ne peut se déplacer que verticalement.")]
    public bool isLadder = false;

    [Tooltip("Si activé, le joueur peut s'accrocher à la grille alors qu'il est dans les airs.")]
    public bool canAttachInAir = true;

    [Tooltip("Point optionnel utilisé pour centrer le joueur sur l'échelle.")]
    public Transform climbAttachPoint;

    [Tooltip("Si activé, les limites sont calculées automatiquement depuis le BoxCollider2D.")]
    public bool autoCompute = true;


    // =========================================================
    // COLLIDER
    // =========================================================

    [Header("Auto Compute From Collider")]

    public float multiplier = 1f;

    public BoxCollider2D box;


    // =========================================================
    // TOP LIMIT
    // =========================================================

    [Header("Top Climb Limit")]

    [Tooltip("Empêche le joueur de s'accrocher au-dessus de cette position.")]
    public Transform topClimbLimit;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (autoCompute)
        {
            ComputeLimits();
        }
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // Si les limites n'ont pas été calculées dans Awake,
        // on peut les recalculer ici.
        if (autoCompute)
        {
            ComputeLimits();
        }
    }


    // =========================================================
    // COMPUTE LIMITS
    // =========================================================

    public void ComputeLimits()
    {
        if (box == null)
        {
            box = GetComponent<BoxCollider2D>();
        }

        if (box == null)
        {
            Debug.LogWarning(
                "[SC_grillage] Aucun BoxCollider2D trouvé sur " +
                gameObject.name
            );

            return;
        }

        Bounds b = box.bounds;

        minX = b.min.x * multiplier;
        maxX = b.max.x * multiplier;

        minY = b.min.y * multiplier;
        maxY = b.max.y * multiplier;
    }


    // =========================================================
    // CLAMP POSITION
    // =========================================================

    public Vector2 ClampPosition(Vector2 pos)
    {
        return new Vector2(
            Mathf.Clamp(
                pos.x,
                minX,
                maxX
            ),

            Mathf.Clamp(
                pos.y,
                minY,
                maxY
            )
        );
    }


    // =========================================================
    // IS INSIDE
    // =========================================================

    public bool IsInside(Vector2 position)
    {
        return
            position.x >= minX &&
            position.x <= maxX &&
            position.y >= minY &&
            position.y <= maxY;
    }


    // =========================================================
    // GET CLIMB CENTER X
    // =========================================================

    public float GetClimbCenterX()
    {
        if (climbAttachPoint != null)
        {
            return climbAttachPoint.position.x;
        }

        if (box != null)
        {
            return box.bounds.center.x;
        }

        return (minX + maxX) / 2f;
    }


    // =========================================================
    // GET TOP Y
    // =========================================================

    public float GetTopY()
    {
        if (topClimbLimit != null)
        {
            return topClimbLimit.position.y;
        }

        return maxY;
    }


    // =========================================================
    // GET BOTTOM Y
    // =========================================================

    public float GetBottomY()
    {
        return minY;
    }


    // =========================================================
    // CAN ATTACH AT POSITION
    // =========================================================

    public bool CanAttachAt(Vector2 playerPosition)
    {
        if (!canAttachInAir)
        {
            // Cette vérification est normalement faite
            // par SC_player avec son GroundCheck.
            return false;
        }

        if (topClimbLimit != null)
        {
            if (playerPosition.y > topClimbLimit.position.y)
                return false;
        }

        return true;
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        Vector3 center = new Vector3(
            (minX + maxX) / 2f,
            (minY + maxY) / 2f,
            0f
        );

        Vector3 size = new Vector3(
            Mathf.Abs(maxX - minX),
            Mathf.Abs(maxY - minY),
            0.05f
        );

        Gizmos.DrawWireCube(
            center,
            size
        );


        // -----------------------------------------------------
        // CENTRE D'ACCROCHAGE
        // -----------------------------------------------------

        float attachX;

        if (climbAttachPoint != null)
        {
            attachX =
                climbAttachPoint.position.x;
        }
        else if (box != null)
        {
            attachX =
                box.bounds.center.x;
        }
        else
        {
            attachX =
                (minX + maxX) / 2f;
        }


        // Ligne centrale de l'échelle
        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(
            new Vector3(
                attachX,
                minY,
                0f
            ),

            new Vector3(
                attachX,
                maxY,
                0f
            )
        );


        // -----------------------------------------------------
        // TOP LIMIT
        // -----------------------------------------------------

        if (topClimbLimit != null)
        {
            Gizmos.color = Color.red;

            Gizmos.DrawLine(
                new Vector3(
                    minX,
                    topClimbLimit.position.y,
                    0f
                ),

                new Vector3(
                    maxX,
                    topClimbLimit.position.y,
                    0f
                )
            );
        }


        // -----------------------------------------------------
        // ATTACH POINT
        // -----------------------------------------------------

        if (climbAttachPoint != null)
        {
            Gizmos.color = Color.cyan;

            Gizmos.DrawWireSphere(
                climbAttachPoint.position,
                0.1f
            );
        }
    }


    // =========================================================
    // GIZMOS SELECTED
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;

        Vector3 center = new Vector3(
            (minX + maxX) / 2f,
            (minY + maxY) / 2f,
            0f
        );

        Vector3 size = new Vector3(
            Mathf.Abs(maxX - minX),
            Mathf.Abs(maxY - minY),
            0.05f
        );

        Gizmos.DrawWireCube(
            center,
            size
        );
    }
}
