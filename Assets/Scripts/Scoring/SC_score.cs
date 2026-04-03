using TMPro;
using UnityEngine;

public class SC_score : MonoBehaviour
{
    public static SC_score Instance;

    [Header("Score Settings")]
    public int score = 0;             // Score réel
    public int displayedScore = 0;    // Score affiché
    public float scoreSpeed = 50f;    // Vitesse d’incrément du score

    [Header("Arcade Style")]
    public int digits = 6;            // Nombre de chiffres affichés (ex: 000123)

    [Header("UI Settings")]
    public TMP_Text scoreText;


    private void Start()
    {
        UpdateScoreUI();

    }
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

    }

    private void Update()
    {
        if (displayedScore < score)
        {
            int increment = Mathf.CeilToInt(scoreSpeed * Time.deltaTime);
            displayedScore += increment;

            if (displayedScore > score)
                displayedScore = score;

            UpdateScoreUI();

            float progress = (float)displayedScore / score;

        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            // Format arcade avec zéros devant
            scoreText.text = displayedScore.ToString("D" + digits);
        }
    }

    public void AddScore(int amount)
    {
        score += amount;
    }
}