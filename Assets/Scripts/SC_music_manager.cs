using UnityEngine;

public class SC_music_manager : MonoBehaviour
{
    public AudioSource music;

    private AudioClip currentClip;

    public static SC_music_manager instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    public void update_music(AudioClip clip)
    {
        update_music(clip, true);
    }

    public void update_music(AudioClip clip, bool looping)
    {
        if (clip == null)
            return;

        // Ne rien faire si c'est déjà la musique actuelle
        if (clip == currentClip && music.isPlaying)
            return;

        currentClip = clip;

        music.loop = looping;
        music.clip = clip;
        music.Play();
    }

    public void stop_music()
    {
        music.Stop();
        currentClip = null;
    }

    public void pause_music()
    {
        music.Pause();
    }

    public void resume_music()
    {
        music.UnPause();
    }
}
