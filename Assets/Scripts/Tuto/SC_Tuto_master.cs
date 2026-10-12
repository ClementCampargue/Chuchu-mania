using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class SC_Tuto_master : MonoBehaviour
{
    public SC_NPC npc;
    public SC_NPC npc_wall;
    public SC_NPC npc_wall_stupido;
    public SC_NPC npc_wall2 ;
    public SC_NPC npc_stomach;
    public SC_NPC npc_eat_creams;
    public SC_NPC npc_eat_creams_finished;
    public SC_NPC npc_eat_creams_failed;
    public SC_starbit_spawning starbits;
    public int tutorial_index;

    public GameObject cream;
    

    public Animator infos;
    public bool wait_talk;
    private bool npc_eat_creamsçhas_talked;
    private bool npc_eat_creams_finishedhas_talked;
    public SC_starbit_spawning spawning;
    public InputActionReference talk;

    private SC_icecream_eat_system eat;
    void Start()
    {
        eat = SC_icecream_eat_system.instance;
        SC_player.instance.canMove = false;
        Invoke("delay", 1);
    }
    void delay()
    {
        npc.StartTalking();

    }
    // Update is called once per frame
    void Update()
    {
        if (wait_talk && talk.action.WasPerformedThisFrame())
        {
            wait_talk = false;
            npc_stomach.StartTalking();

        }
        if(eat.currrent_ice_cream>= 10 && !npc_eat_creamsçhas_talked)
        {
            npc_eat_creamsçhas_talked = true;
            npc_eat_creams.StartTalking();
        }
        if(eat.selectedCreams.Count >= 10 && !npc_eat_creams_finishedhas_talked && eat.isEating)
        {
            npc_eat_creams_finishedhas_talked = true;
            Invoke("delay_eat_finished", 1);
        }
        if(eat.selectedCreams.Count < 10 && npc_eat_creamsçhas_talked && eat.isEating)
        {
            npc_eat_creamsçhas_talked = false;
            Invoke("delay_eat_failed", 0.5f);
        }
    }

    void delay_eat_finished()
    {
        npc_eat_creams_finished.StartTalking();
    }

    void delay_eat_failed()
    {
        npc_eat_creams_failed.StartTalking();
    }

    public void spawn_creams()
    {
        Debug.Log("ça marche");
        eat.SpawnMaxIceCreams(cream);
    }

    public void next_tuto()
    {
        tutorial_index++;
        if(tutorial_index == 1)
        {
            starbits.Spawn_collectibles();
        }
        else if (tutorial_index == 2)
        {

        }
    }

    public void spawn_cream()
    {
        spawning.Spawn_collectibles();
    }

    public void show_cream()
    {
        infos.SetTrigger("cream");
    }

    public void show_hearts()
    {
        infos.SetTrigger("hearts");
    }

    public void show_stomach()
    {
        Invoke("delay_canact", 0.5f);
        infos.SetTrigger("stomach");
    }

    void delay_canact()
    {
        wait_talk = true;
    }

    public void show_info()
    {
        infos.SetTrigger("show");
    }

    public void hide_info()
    {
        infos.SetTrigger("hide");
    }
}
