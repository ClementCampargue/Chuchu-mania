using UnityEngine;

public class SC_level_master : MonoBehaviour
{
    public AudioClip music;


    void Start()
    {
        SC_music_manager.instance.update_music(music);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
