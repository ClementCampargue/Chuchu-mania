using UnityEngine;

public class SC_moving_platorm_parent : MonoBehaviour
{
    private SC_player player;
    private SC_game_master gm;

    private void Start()
    {
        player = SC_player.instance;
        gm = SC_game_master.instance;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && player.isGrounded && player.rb.linearVelocity.y<0.1f&& player.rb.linearVelocity.y>-0.1f)
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