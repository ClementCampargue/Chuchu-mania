using System;
using UnityEngine;

public class SC_tuto_wall : MonoBehaviour
{
    public Animator anim;
    public SC_Tuto_master tuto;
    bool cancollide;

    public int index;
    public float time_before_help;
    public bool only_help_once;
    private bool once;
    float timer;
    void Start()
    {
        cancollide = true;
    }


    // Update is called once per frame
    void Update()
    {
        if(index == 1)
        {
            if (SC_player.instance.canMove)
            {
                timer += Time.deltaTime;
            }
            if (timer > time_before_help) 
            {
                if (only_help_once && once)
                {
                    return;
                }
                timer = 0;
                tuto.npc_wall_stupido.StartTalking();

                once = true;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && cancollide)
        {
            if (index == 0) 
            {
                index++;
                cancollide = false;
                anim.enabled = true;

            }
            else
            {
                tuto.npc_wall2.StartTalking();
                Destroy(this);
            }
        }
    }

    public void delay()
    {
        tuto.npc_wall.StartTalking();
        cancollide = true;
    }
}
