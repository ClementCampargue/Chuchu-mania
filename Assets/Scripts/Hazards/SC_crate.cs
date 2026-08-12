using UnityEngine;

public class SC_crate : MonoBehaviour
{
    public Rigidbody2D rb;

    private Vector2 added_velocity;
    private Vector2 added_velocity_;

    private float speed = 1f;
    public float maxSpeed = 10f;

    private bool isGrounded;
    public bool damage_player;
    private void FixedUpdate()
    {
        added_velocity_ = Vector2.Lerp(added_velocity_, added_velocity, 0.2f);

        Vector2 vel = rb.linearVelocity;

        if (isGrounded)
        {
            vel.x += added_velocity_.x * speed;
            vel.x = Mathf.Clamp(vel.x, -maxSpeed, maxSpeed);
        }
        else
        {
            vel.x = 0f; // vitesse horizontale nulle en l'air
        }

        rb.linearVelocity = new Vector2(vel.x, vel.y);
    }

    public void SetGroundVelocity(Vector2 vel)
    {
        added_velocity = vel;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        isGrounded = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (SC_icecream_eat_system.instance.isPowerUpActive)
            {
                SC_player.instance.anim.SetTrigger("Punch");
                die();
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        isGrounded = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (damage_player)
            {
                SC_player.instance.TakeDamage(1,1,transform.position);
                Destroy(gameObject);
            }
            else if(!isGrounded && SC_player.instance.isGrounded)
            {
                SC_player.instance.stun_player();
                Destroy(gameObject);
            }
         
        }
        if (collision.CompareTag("Lava"))
        {
            Destroy(gameObject);

        }
    }

    public void die()
    {
        Destroy(gameObject);
    }
}