using UnityEngine;
using UnityEngine.InputSystem;

public class SC_pilar : MonoBehaviour
{

    public SC_player player;
    public Transform snap_point;

    public bool statue_mode_;

    public InputActionReference input;
    public SC_alarm_system alarm;
    void Start()
    {
        player = SC_player.instance;
        alarm = SC_alarm_system.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        if (statue_mode_)
        {
            if (input.action.WasPressedThisFrame())
            {
                quit_statue_mode();
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if(collision.CompareTag("Player") && player.IsAnimationPlaying("land") && !SC_alarm_system.Instance.alarmActive&& !statue_mode_ && player.transform.position.y> snap_point.position.y -0.1f)
        {
            statue_mode();
        }
    }

    public void statue_mode()
    {
        input.action.Enable();
        statue_mode_ = true;
        player.collision.enabled = false;
        player.rb.constraints = RigidbodyConstraints2D.FreezeAll;
        alarm.hidden = true;
        player.anim.SetTrigger("statue");
            player.rb.linearVelocity =
            Vector2.zero;
        player.enabled = false;
        player.canMove = false;
        player.transform.position = snap_point.position;
    }

    public void quit_statue_mode()
    {
        alarm.hidden = false;
        player.rb.constraints =
            RigidbodyConstraints2D.FreezeRotation;
        player.collision.enabled = true;
        CancelInvoke("delay");
        Invoke("delay", 0.5f);
        player.enabled = true;
        player.canMove = true;
        player.TryJump();
    }

    void delay()
    {
        statue_mode_ = false;
    }
}
