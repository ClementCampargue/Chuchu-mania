using UnityEngine;

public class SC_move_belt : MonoBehaviour
{
    [Header("Conveyor Settings")]
    public float speed = 3f;

    public Vector2 direction =
        Vector2.right;

    private SC_player player;

    public Material mat;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (mat != null)
        {
            mat.SetVector(
                "_Speed",
                Vector2.zero
            );
        }
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        player = SC_player.instance;

        if (mat != null)
        {
            mat.SetVector(
                "_Speed",
                new Vector2(-1f, 0f)
            );
        }
    }

    // =========================================================
    // STAY
    // =========================================================

    private void OnTriggerStay2D(
        Collider2D collision)
    {
        // -----------------------------------------------------
        // PLAYER
        // -----------------------------------------------------

        if (collision.CompareTag("Player"))
        {
            if (player == null)
                return;

            if (player.isGrounded &&
                player.rb.linearVelocity.y <= 0.01f)
            {
                player.SetGroundVelocity(
                    direction.normalized *
                    speed
                );
            }
        }

        // -----------------------------------------------------
        // CRATE
        // -----------------------------------------------------

        if (collision.CompareTag("Crate"))
        {
            SC_crate crate =
                collision.GetComponent<SC_crate>();

            if (crate != null)
            {
                crate.SetGroundVelocity(
                    direction.normalized *
                    speed
                );
            }
        }
    }

    // =========================================================
    // EXIT
    // =========================================================

    private void OnTriggerExit2D(
        Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (player != null)
            {
                player.ClearGroundVelocity();
            }
        }

        if (collision.CompareTag("Crate"))
        {
            SC_crate crate =
                collision.GetComponent<SC_crate>();

            if (crate != null)
            {
                crate.SetGroundVelocity(
                    Vector2.zero
                );
            }
        }
    }
}