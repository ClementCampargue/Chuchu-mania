using System.Collections.Generic;
using UnityEngine;

public class SC_move_belt : MonoBehaviour
{
    [Header("Conveyor Settings")]
    public float speed = 3f;
    public Vector2 direction = Vector2.right;

    private SC_player player;
    public Material mat;
    private void Start()
    {
        player = SC_player.instance;
        mat.SetVector("_Speed", new Vector2(-1,0));
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && player.isGrounded && player.rb.linearVelocity.y <= 0.01f)
        {
            player.SetGroundVelocity(direction * speed);
        }
        if (collision.CompareTag("Crate"))
        {
            SC_crate crate = collision.GetComponent<SC_crate>();
            crate.SetGroundVelocity(direction * speed);
        }
    }
    
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            player.SetGroundVelocity(Vector2.zero);
        }
        if (collision.CompareTag("Crate"))
        {
            SC_crate crate = collision.GetComponent<SC_crate>();
            crate.SetGroundVelocity(Vector2.zero);
        }
    }


}
