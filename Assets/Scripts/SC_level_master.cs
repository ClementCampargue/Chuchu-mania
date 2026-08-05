using UnityEngine;

public class SC_level_master : MonoBehaviour
{
    public AudioClip music;
    public bool overlay_on = true;
    public bool level;
    public Transform spawn;
    public static SC_level_master instance;
    public float limits;
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        SC_music_manager.instance.update_music(music);
        GameObject.Find("Overlay").transform.GetChild(0).gameObject.SetActive(overlay_on);
        SC_player.instance.enabled = true;
        if (level)
        {
            SC_player.instance.gameObject.SetActive(true);
            SC_player.instance.rb.linearVelocity = Vector2.zero;
            SC_player.instance.transform.position = spawn.position;
        }
        else
        {
            SC_player.instance.gameObject.SetActive(false);

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
