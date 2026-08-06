using UnityEngine;
using UnityEngine.SceneManagement;

public class SC_loose_screen : MonoBehaviour
{
    public AudioClip clip;
    public void ReloadCurrentScene()
    {
        Time.timeScale = 1.0f;  
        // Récupère le nom de la scène actuelle
        Scene currentScene = SceneManager.GetActiveScene();
        // Recharge la scène
        SceneManager.LoadScene(currentScene.name);
    }


    public void revive()
    {
        SC_player.instance.Revive();
        SC_screenshot_transition.instance.Capture(SceneManager.GetActiveScene().name);
        gameObject.SetActive(false);
    }
    public void music()
    {
        SC_music_manager.instance.update_music(clip, false);

    }
    public void give_up()
    {
        SC_screenshot_transition.instance.Capture("MoneyScene");
        gameObject.SetActive(false);
    }
}
