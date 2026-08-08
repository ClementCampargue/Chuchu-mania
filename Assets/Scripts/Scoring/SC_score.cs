using TMPro;
using UnityEngine;

public class SC_score : MonoBehaviour
{
    public static SC_score Instance;

    [Header("Score Settings")]
    public int score = 0;
    public int displayedScore = 0;

    public float scoreSpeed = 50f;

    [Header("Arcade Style")]
    public int digits = 6;

    [Header("UI Settings")]
    public TMP_Text scoreText;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        UpdateScoreUI();
    }

    private void Update()
    {

        if (displayedScore < score)
        {
            int increment =
                Mathf.CeilToInt(
                    scoreSpeed * Time.deltaTime
                );

            displayedScore += increment;

            if (displayedScore > score)
                displayedScore = score;

            UpdateScoreUI();
        }
        else if (displayedScore > score)
        {
            int decrement =
                Mathf.CeilToInt(
                    scoreSpeed * Time.deltaTime
                );

            displayedScore -= decrement;

            if (displayedScore < score)
                displayedScore = score;

            UpdateScoreUI();
        }
    }

    public void reset_score_level()
    {
        score = 0;
        displayedScore = 0;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text =
                displayedScore.ToString(
                    "D" + digits
                );
        }
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreUI();
    }
}