using UnityEngine;

public class SC_cage : MonoBehaviour
{

    private SC_icecream_eat_system system;

    public int Health = 1;
    public GameObject fire_system;
    private SC_win_screen win_screen;
    public SC_juiciness juice_damage;
    public SC_juiciness juice_death;

    public Material player_green;
    private Material default_mat;
    private SpriteRenderer spr;
    public Animator anim;
    public AudioClip clip;
    void Start()
    {
        system = SC_icecream_eat_system.instance;
        spr = SC_player.instance.spriteRendererPower;
        default_mat = spr.material;
        win_screen = SC_win_screen.instance;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && system.isPowerUpActive && Health !=0)
        {
            Debug.Log("die");
            die();
        }
    }

    void die()
    {
        Health--;
        if (Health == 0)
        {
            anim.SetTrigger("Die");
            fire_system.SetActive(false);
            juice_death.PlayJuice();
            Time.timeScale = 0.25f;
            spr.material = player_green;
            SC_player.instance.enabled = false;
            SC_player.instance.collider.enabled = false;
            SC_player.instance.rb.gravityScale = 0;
            SC_player.instance.rb.constraints = RigidbodyConstraints2D.FreezeAll;
            SC_player.instance.anim_powerup.SetTrigger("End");
            SC_music_manager.instance.stop_music();
        }
        else
        {
            anim.SetTrigger("Damage");
            juice_damage.PlayJuice();

        }
    }

    public void RestorePlayerMat()
    {
        spr.material = default_mat;
        SC_player.instance.gameObject.SetActive(false);
    }

    public void win_sc()
    {
        win_screen.Start_screen();
        Time.timeScale = 0f;
    }

    public void play_music()
    {
        SC_music_manager.instance.update_music(clip,false);
    }
}
