using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SC_loose_screen : MonoBehaviour
{
    public AudioClip clip;
    public int number_of_death;
    public List<int> score_losses;
    public TextMeshPro score_loss;

    public void ReloadCurrentScene()
    {
        SC_scursorManager.instance.disable_cursor();

        Time.timeScale = 1.0f;

        // Récupère le nom de la scène actuelle
        Scene currentScene = SceneManager.GetActiveScene();

        // Recharge la scène
        SceneManager.LoadScene(currentScene.name);
    }

    public void revive()
    {
        SC_scursorManager.instance.disable_cursor();
        int index = Mathf.Min(number_of_death, score_losses.Count - 1);

        SC_score.Instance.score -= score_losses[index];
        if (SC_score.Instance.score < 0)
        {
            SC_score.Instance.score = 0;
        }
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
        SC_scursorManager.instance.disable_cursor();

        if (PlayerPrefs.GetInt("Score") == 0)
        {
            SC_screenshot_transition.instance.Capture("HUB");
        }
        else
        {
            SC_screenshot_transition.instance.Capture("MoneyScene");
        }
        gameObject.SetActive(false);
    }

    public void OnEnable_()
    {
        // Évite une erreur si la liste est vide
        if (score_losses.Count == 0)
            return;

        // Reste sur le dernier élément une fois arrivé au maximum
        int index = Mathf.Min(number_of_death, score_losses.Count - 1);

        score_loss.text = score_losses[index].ToString();

        // N'incrémente plus si on est déjà au dernier élément
        if (number_of_death < score_losses.Count - 1)
        {
            number_of_death++;
        }
    }

    public void show_cursor()
    {
        SC_scursorManager.instance.enable_cursor();
    }
}
