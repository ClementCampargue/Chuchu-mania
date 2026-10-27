using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class SC_Tuto_master : MonoBehaviour
{
    public AudioClip tuto_zic;
    public SC_NPC npc;
    public SC_NPC npc_wall;
    public SC_NPC npc_wall_stupido;
    public SC_NPC npc_wall2 ;
    public SC_NPC npc_eat_creams;
    public SC_NPC npc_eat_creams_finished;
    public SC_NPC npc_eat_creams_failed;
    public SC_NPC npc_power_up;
    public SC_starbit_spawning starbits;
    public int tutorial_index;

    public GameObject cream;
    

    public Animator infos;
    public bool wait_talk;
    private bool npc_eat_creamsçhas_talked;
    private bool npc_eat_creams_finishedhas_talked;
    private bool npc_power_up_done;
    public SC_starbit_spawning spawning;
    public InputActionReference talk;

    private SC_icecream_eat_system eat;

    public GameObject enemy;
    public GameObject spawner;
    public GameObject wall;
    void Start()
    {
        tuto_zic.LoadAudioData();

        eat = SC_icecream_eat_system.instance;
        SC_player.instance.canMove = false;
        Invoke("delay", 1);
        SC_icecream_eat_system.instance.caneat = false;
        SC_icecream_eat_system.instance.canfillup = false;
    }
    void delay()
    {
        npc.StartTalking();

    }
    // Update is called once per frame
    void Update()
    {
        if(!npc_power_up_done)
        {
            if (eat.isPowerUpActive) {
                npc_power_up_done = true;
                npc_power_up.StartTalking();
            }
        }
        if(eat.currrent_ice_cream>= 10 && !npc_eat_creamsçhas_talked)
        {
            npc_eat_creamsçhas_talked = true;
            npc_eat_creams.StartTalking();
        }
        if(eat.selectedCreams.Count >= 10 && !npc_eat_creams_finishedhas_talked && eat.isEating)
        {
            SC_icecream_eat_system.instance.canfillup = true;
            npc_eat_creams_finishedhas_talked = true;
            CancelInvoke("delay_eat_finished");
            Invoke("delay_eat_finished", 1);
        }
        if(eat.selectedCreams.Count < 10 && npc_eat_creamsçhas_talked && eat.isEating && !npc_eat_creams_finishedhas_talked)
        {
            delay_eat_failed();
        }
    }

    void delay_eat_finished()
    {
        npc_eat_creams_finishedhas_talked = true;
        npc_eat_creams_finished.StartTalking();
    }

    void delay_eat_failed()
    {
        npc_eat_creams_failed.StartTalking();
    }

    public void spawn_creams()
    {
        if (npc_eat_creams_finishedhas_talked)
        {
            return;
        }
        eat.SpawnMaxIceCreams(cream);
    }

    public void start_level()
    {
        SC_music_manager.instance.update_music(tuto_zic);
        //SC_music_manager.instance.resume_music();
        spawning.manual_respawn = false;
        spawner.SetActive(true);
        wall.SetActive(false);
    }

    public void spawn_enemy()
    {
        enemy.SetActive(true);
    }


    public void spawn_cream()
    {
        spawning.Spawn_collectibles();
    }

    public void show_cream()
    {
        SC_icecream_eat_system.instance.caneat = true;

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
