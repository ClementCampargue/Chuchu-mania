using TMPro;
using UnityEngine;

public class SC_score : MonoBehaviour
{
    public static SC_score Instance;

    [Header("Score Settings")]
    public int score = 0;             // Score réel
    public int displayedScore = 0;    // Score affiché
    public float scoreSpeed = 50f;    // Vitesse d’incrément du score

    [Header("UI Settings")]
    public TMP_Text scoreText;        // TextMeshPro pour afficher le score
    public float maxScaleAmplitude = 0.3f; // Combien le texte peut grossir max pendant l’incrément

    private Vector3 baseScale;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (scoreText != null)
            baseScale = scoreText.transform.localScale; // sauvegarder la taille de base
    }

    private void Update()
    {
        if (displayedScore < score)
        {
            // Calculer l’incrément pour ce frame
            int increment = Mathf.CeilToInt(scoreSpeed * Time.deltaTime);
            displayedScore += increment;

            if (displayedScore > score)
                displayedScore = score;

            // Mettre à jour le texte
            scoreText.text = displayedScore.ToString();

            // Calculer le pourcentage de progression pour l’effet de scale
            float progress = (float)(displayedScore) / score; // 0 -> 1
            float scaleMultiplier = 1f + progress * maxScaleAmplitude;

            // Appliquer le scale pendant l’incrément
            scoreText.transform.localScale = baseScale * scaleMultiplier;
        }
        else
        {
            // Revenir à la taille normale une fois que l’incrément est terminé
            scoreText.transform.localScale = baseScale;
        }
    }

    public void AddScore(int amount)
    {
        score += amount;
    }
}