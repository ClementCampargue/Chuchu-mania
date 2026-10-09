using UnityEngine;
using UnityEngine.InputSystem;

public class SC_pilar : MonoBehaviour
{
    public SC_player player;
    public Transform snap_point;

    public bool statue_mode_;

    public InputActionReference input;
    public InputActionReference eat;
    public SC_alarm_system alarm;

    [Header("Sprites aléatoires")]
    public Sprite[] statue_sprites;
    public BoxCollider2D collider_;

    void Start()
    {
        player = SC_player.instance;
        alarm = SC_alarm_system.Instance;
    }

    void Update()
    {
        if (statue_mode_)
        {
            if (input.action.WasPressedThisFrame())
            {
                quit_statue_mode();
            }
            if (eat.action.WasPressedThisFrame())
            {
                quit_statue_mode();
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")
            && player.IsAnimationPlaying("land")
            && !SC_alarm_system.Instance.alarmActive
            && !statue_mode_
            && player.transform.position.y > snap_point.position.y - 0.1f)
        {
            statue_mode();
        }
    }

    public void statue_mode()
    {
        input.action.Enable();
        player.anim.enabled = false;
        statue_mode_ = true;
        player.collision.enabled = false;
        player.rb.constraints = RigidbodyConstraints2D.FreezeAll;

        alarm.hidden = true;

        Invoke("delay_", 0.1f);
        player.rb.linearVelocity = Vector2.zero;
        player.enabled = false;
        player.canMove = false;
        player.transform.position = snap_point.position;
    }

    void delay_()
    {
        if (statue_sprites != null && statue_sprites.Length > 0)
        {
            int randomIndex = Random.Range(0, statue_sprites.Length);
            player.spriteRenderer.sprite = statue_sprites[randomIndex];
        }

    }

    public void quit_statue_mode()
    {
        alarm.hidden = false;
        player.anim.enabled = true;

        player.rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        player.collision.enabled = true;

        CancelInvoke("delay");
        Invoke("delay", 0.5f);

        player.enabled = true;
        player.canMove = true;
        collider_.enabled = false;
    }

    void delay()
    {
        collider_.enabled = true;
        statue_mode_ = false;
    }
}
