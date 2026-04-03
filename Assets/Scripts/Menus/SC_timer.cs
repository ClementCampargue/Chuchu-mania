using TMPro;
using UnityEngine;

public class SC_timer : MonoBehaviour
{
    public TextMeshPro timerText; // UI Text à assigner dans l'inspecteur
    private float elapsedTime = 0f;

    void Update()
    {
        // Incrémente le temps
        elapsedTime += Time.deltaTime;

        // Convertit en secondes entières
        int seconds = Mathf.FloorToInt(elapsedTime);

        // Limite à 999 si tu veux rester sur 3 chiffres
        seconds = Mathf.Clamp(seconds, 0, 999);

        // Format en 3 chiffres (ex: 005, 042, 123)
        timerText.text = seconds.ToString("D3");
    }
}
