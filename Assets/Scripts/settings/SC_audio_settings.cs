
using UnityEngine;
using UnityEngine.Audio;

public class SC_audio_settings : MonoBehaviour
{
    public AudioMixer mixer;

    void Start()
    {
        update_sound();
    }

    public void update_sound()
    {
        float music = PlayerPrefs.GetFloat("Music", 1f);
        float sound = PlayerPrefs.GetFloat("Sound", 1f);

        // Master Volume
        mixer.SetFloat("MasterVolume", music <= 0f ? -80f : Mathf.Log10(PlayerPrefs.GetFloat("Master", 1f)) * 20f);

        // Music Volume
        mixer.SetFloat("MusicVolume", music <= 0f ? -80f : Mathf.Log10(music) * 20f);

        // Sound Effects Volume
        mixer.SetFloat("SFXVolume", sound <= 0f ? -80f : Mathf.Log10(sound) * 20f);
    }
}
