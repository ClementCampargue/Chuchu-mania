using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI; // Pour Image

public class SC_rank_system : MonoBehaviour
{
    [Header("Score thresholds")]
    public float score_rank_D = 0;
    public float score_rank_C = 1000;
    public float score_rank_B = 3000;
    public float score_rank_A = 6000;
    public float score_rank_S = 10000;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI rankText;
    public Animator rankAnimator;

    [Header("Rank Icons")]
    public Image rankImage;           // Image UI pour le rang
    public Sprite rankDImage;
    public Sprite rankCImage;
    public Sprite rankBImage;
    public Sprite rankAImage;
    public Sprite rankSImage;

    private SC_score score;

    private float displayedScore = 0;
    private float targetScore = 0;

    private string currentRank = "D";

    void Start()
    {
        score = SC_score.Instance;
        targetScore = score.score;

        // Initialisation affichage
        scoreText.text = FormatScore(0);
        rankText.text = currentRank;
        rankImage.sprite = rankDImage; // Image initiale

        StartCoroutine(AnimateScore());
    }

    void Update()
    {
        targetScore = score.score;
    }

    IEnumerator AnimateScore()
    {
        while (true)
        {
            if (displayedScore < targetScore)
            {
                // Animation fluide
                displayedScore += Time.deltaTime * 2000f;

                if (displayedScore > targetScore)
                    displayedScore = targetScore;

                // Affichage arcade
                scoreText.text = FormatScore(displayedScore);

                // Update rank
                UpdateRank(displayedScore);
            }

            yield return null;
        }
    }

    void UpdateRank(float scoreValue)
    {
        string newRank = "D";
        Sprite newRankSprite = rankDImage;

        if (scoreValue >= score_rank_S)
        {
            newRank = "S";
            newRankSprite = rankSImage;
        }
        else if (scoreValue >= score_rank_A)
        {
            newRank = "A";
            newRankSprite = rankAImage;
        }
        else if (scoreValue >= score_rank_B)
        {
            newRank = "B";
            newRankSprite = rankBImage;
        }
        else if (scoreValue >= score_rank_C)
        {
            newRank = "C";
            newRankSprite = rankCImage;
        }

        if (newRank != currentRank)
        {
            currentRank = newRank;
            rankText.text = currentRank;

            // Changer l'image
            if (rankImage != null)
            {
                rankImage.sprite = newRankSprite;
            }

            // Animation de rank
            if (rankAnimator != null)
            {
                rankAnimator.SetTrigger("RankUp");
            }

            Debug.Log("Rank UP : " + currentRank);
        }
    }

    string FormatScore(float scoreValue)
    {
        int scoreInt = Mathf.FloorToInt(scoreValue);

        // 6 chiffres avec zéros devant
        return scoreInt.ToString("D6");
    }
}