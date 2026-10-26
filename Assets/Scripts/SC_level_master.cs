using UnityEngine;
using UnityEngine.Audio;

public class SC_level_master : MonoBehaviour
{
    public string next_scene;
    public AudioClip music;
    public bool overlay_on = true;
    public bool level;
    public Transform spawn;
    public static SC_level_master instance;

    [Header("Limites X")]
    public Vector2 limits = new Vector2(-10f, 10f);

    [Header("Discord Rich Presence")]
    public string discordState = "";
    public string discordLargeImage = "";

    private SC_player player;
    public SO_level level_;
    public AudioMixer group;
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        player = SC_player.instance;
        group.SetFloat("LevelVolume", 0f);
        if(music != null)
        {
            SC_music_manager.instance.update_music(music);
        }
        else
        {
            SC_music_manager.instance.pause_music();
        }

        GameObject.Find("Overlay")
            .transform.GetChild(0)
            .gameObject.SetActive(overlay_on);

        SC_player.instance.moveSpeed =
            SC_player.instance.base_speed;
        SC_score.Instance.reset_score_level();
        SC_timer.instance.reset_timer();
        SC_timer.instance.pause();

        if (level)
        {
            PlayerPrefs.SetInt("nodamage", 0);
            PlayerPrefs.SetInt("angry_fish", 0);
            PlayerPrefs.SetInt("museum", 0);
            PlayerPrefs.SetInt("museum_no_hiding", 0);
            PlayerPrefs.SetInt("fish_no_eat", 0);
            PlayerPrefs.SetInt("currentlevel", PlayerPrefs.GetInt("currentlevel") + 1);

            player.Revive();
            player.gameObject.SetActive(true);

            player.rb.linearVelocity = Vector2.zero;

            player.transform.position = spawn.position;

            Invoke("delay", 0.1f);
        }
        else
        {
            PlayerPrefs.SetInt("currentlevel", 0);

            player.gameObject.SetActive(false);
        }

        SC_player.instance.rb.gravityScale = 3;

        if (SC_discord_manager.Instance != null)
        {
            SC_discord_manager.Instance.ChangeActivity(
                discordState,
                discordLargeImage
            );
        }
    }

    private void delay()
    {
        SC_player.instance.enabled = false;

        player.gameObject.SetActive(true);

        player.enabled = true;
        player.anim.SetTrigger("Start");
        player.start.PlayJuice();
    }

    private void Update()
    {
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        // Ligne de limite gauche
        Gizmos.DrawLine(
            new Vector3(limits.x, -100f, 0f),
            new Vector3(limits.x, 100f, 0f)
        );

        // Ligne de limite droite
        Gizmos.DrawLine(
            new Vector3(limits.y, -100f, 0f),
            new Vector3(limits.y, 100f, 0f)
        );
    }
}