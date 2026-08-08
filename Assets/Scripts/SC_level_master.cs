using UnityEngine;

public class SC_level_master : MonoBehaviour
{
    public AudioClip music;
    public bool overlay_on = true;
    public bool level;
    public Transform spawn;
    public static SC_level_master instance;
    public float limits;
    private SC_player player;
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        player = SC_player.instance;
        SC_music_manager.instance.update_music(music);
        GameObject.Find("Overlay").transform.GetChild(0).gameObject.SetActive(overlay_on);
        SC_player.instance.enabled = false;
        SC_timer.instance.reset_timer();
        SC_timer.instance.pause();
        if (level)
        {
            player.Revive();

            player.gameObject.SetActive(true);
            player.rb.linearVelocity = Vector2.zero;
            player.transform.position = spawn.position;
            Invoke("delay", 0.1f);
        }
        else
        {
            player.gameObject.SetActive(false);
        }
        SC_player.instance.rb.gravityScale = 3;
    }

    void delay()
    {
        player.gameObject.SetActive(true);
        player.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
