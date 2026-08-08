using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SC_winscreen_scoring : MonoBehaviour
{
    private SC_score score;
    private sc_health_system health;
    private SC_timer time;

    public TextMeshPro life_score;
    public TextMeshPro time_score;
    public TextMeshPro time_remaining;
    public TextMeshPro final_score;

    public SpriteRenderer rank;
    public List<Sprite> ranks;
    public List<int> rank_scores;
    public List<SpriteRenderer> hearts;

    public int score_per_heart = 5000;
    public int score_per_time = 50;

    private int base_score;
    private int time_bonus;
    private int health_bonus;

    public Animator anim;

    // Durée TOTALE de l'animation du décompte, en secondes.
    // Peu importe le nombre de points à ajouter, l'animation
    // prendra toujours cette durée.
    public float score_animation_duration = 2f;

    void Start()
    {
        time = SC_timer.instance;
        score = SC_score.Instance;
        health = sc_health_system.instance;
    }

    public void calculate_score()
    {
        base_score = score.score;

        time_bonus = (int)(time.base_time * score_per_time);
        time_score.text = time_bonus.ToString();

        health_bonus = score_per_heart * health.current_health;

        foreach (SpriteRenderer spr in hearts)
        {
            spr.enabled = false;
        }

        for (int i = 0; i < hearts.Count; i++)
        {
            hearts[i].enabled = i < health.current_health;
        }

        life_score.text = health_bonus.ToString();

        // Affiche le score de base avant le décompte
        final_score.text = base_score.ToString();
    }

    public void calculate_time()
    {
        pause();

        // Le bonus sera ajouté progressivement
        // sur une durée fixe.
        StartCoroutine(AddBonusToScore(time_bonus, time_score));
    }

    public void calculate_haelth()
    {
        pause();

        // Le bonus sera ajouté progressivement
        // sur une durée fixe.
        StartCoroutine(AddBonusToScore(health_bonus, life_score));
    }

    private IEnumerator AddBonusToScore(int bonus, TextMeshPro bonusText)
    {
        if (bonus <= 0)
        {
            resume();
            yield break;
        }

        int startingScore = base_score;
        float elapsedTime = 0f;

        while (elapsedTime < score_animation_duration)
        {
            elapsedTime += Time.deltaTime;

            // Progression de 0 à 1 pendant la durée définie
            float progress = Mathf.Clamp01(elapsedTime / score_animation_duration);

            // Nombre de points qui doivent avoir été ajoutés
            int addedScore = Mathf.FloorToInt(bonus * progress);

            // Évite les doublons et permet de reprendre correctement
            int currentAdded = base_score - startingScore;

            if (addedScore > currentAdded)
            {
                int pointsToAdd = addedScore - currentAdded;

                base_score += pointsToAdd;

                // Bonus restant
                int remaining = bonus - addedScore;

                bonusText.text = remaining.ToString();
                final_score.text = base_score.ToString();
            }

            yield return null;
        }

        // Garantit que le bonus entier est ajouté à la fin
        base_score = startingScore + bonus;

        bonusText.text = "0";
        final_score.text = base_score.ToString();

        // Une fois le bonus entièrement transféré,
        // on reprend l'animation.
        resume();
    }

    public void pause()
    {
        anim.speed = 0;
    }

    public void resume()
    {
        anim.speed = 1;
    }

    public void CalculateRank()
    {
        if (ranks == null || ranks.Count == 0 || rank_scores == null || rank_scores.Count == 0)
            return;

        int bestRank = 0;

        // Cherche le rang correspondant au score
        for (int i = 0; i < rank_scores.Count; i++)
        {
            if (base_score >= rank_scores[i])
            {
                bestRank = i;
            }
        }

        // Évite de dépasser le nombre de sprites disponibles
        bestRank = Mathf.Clamp(bestRank, 0, ranks.Count - 1);

        rank.sprite = ranks[bestRank];
    }
}
