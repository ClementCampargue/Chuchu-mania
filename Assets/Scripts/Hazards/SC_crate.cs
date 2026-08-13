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
    public Animator anim;
    public SC_juiciness land_juice;
    public SC_juiciness juice;
    public SC_crate_parent parent;
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
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
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
        if (collision.gameObject.CompareTag("Ground"))
        {
            land_juice.PlayJuice();
            anim.SetTrigger("Land");
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if(SC_player.instance.burning)
            {
                die();
            }

            if (damage_player)
            {
                SC_player.instance.TakeDamage(1,new Vector2(1,1),transform.position);
                die();
            }
            else if(!isGrounded && SC_player.instance.isGrounded)
            {
                SC_player.instance.stun_player();
                die();
            }

        }
        if (collision.CompareTag("Lava"))
        {
            die();

        }
    }

    public void die()
    {
        parent.destroy();
        juice.PlayJuice();
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        anim.SetTrigger("Die");
    }

    public void _destroy()
    {
        Destroy(gameObject);

    }
}