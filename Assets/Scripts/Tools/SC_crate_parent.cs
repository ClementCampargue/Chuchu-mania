using UnityEngine;

public class SC_crate_parent : MonoBehaviour
{
    private SC_player player;
    private SC_game_master gm;

    private Vector3 lastPosition;
    private Vector3 platformMovement;

    private void Start()
    {
        player = SC_player.instance;
        gm = SC_game_master.instance;

        lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        // Fonctionne avec ou sans Rigidbody2D
        platformMovement = transform.position - lastPosition;
        lastPosition = transform.position;

        // Si le joueur est dessus, on applique le déplacement
        if (player != null && player.transform.parent == transform)
        {
            player.transform.position += platformMovement;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") &&
            player.isGrounded &&
            Mathf.Abs(player.rb.linearVelocity.y) < 0.1f)
        {
            player.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            player.transform.SetParent(gm.transform);
        }
    }
}