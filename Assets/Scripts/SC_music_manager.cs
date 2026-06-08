using UnityEngine;

public class SC_music_manager : MonoBehaviour
{
    public AudioSource music;
    private AudioClip currentClip;
    public static SC_music_manager instance;
    private void Awake()
    {
        instance = this;
    }
    public void update_music(AudioClip clip)
    {
        if (clip != currentClip) 
        {
            music.clip = clip;
            music.Play();
        }
    }
}
