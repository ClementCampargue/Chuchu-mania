using UnityEngine;
using UnityEngine.SceneManagement;

public class SC_loose_screen : MonoBehaviour
{

    public void ReloadCurrentScene()
    {
        Time.timeScale = 1.0f;  
        // Récupère le nom de la scène actuelle
        Scene currentScene = SceneManager.GetActiveScene();
        // Recharge la scène
        SceneManager.LoadScene(currentScene.name);
    }
}
