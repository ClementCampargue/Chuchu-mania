using System.Collections;
using TMPro;
using UnityEngine;

public class SC_tirelire : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer star;
    public Transform tongue;
    public ParticleSystem stars;
    public Animator cat_anim;
    public Animator screen_anim;
    public AudioSource starAudio;

    [Header("Audio")]
    public AudioClip particleSound; 

    [Header("Score")]
    public int total_score;
    public int current_score;

    [Tooltip("Nombre de points transférés par seconde")]
    public float decrease_speed = 100f;

    [Tooltip("Une particule émise tous les X points transférés")]
    public int score_per_star = 10;

    [Header("Star")]
    public Color star_color_max;

    [Header("Tongue")]
    [Tooltip("Déplacement maximal sur X")]
    public float tongue_max_abs = 0.5f;

    [Header("Animation")]
    public Vector2 anim_speed_range = new Vector2(1f, 2f);

    [Header("UI")]
    public TextMeshPro score_toadd;
    public TextMeshPro score_gained;
    public TextMeshPro score_gained2;

    private bool counting;

    private Vector3 tongueBasePos;
    private int particleCounter;
    private float scoreAccumulator;

    private Color starOriginalColor;



    private void Start()
    {
        tongueBasePos = tongue.localPosition;

        starOriginalColor = star.color;

        cat_anim.speed = anim_speed_range.x;
        cat_anim.enabled = false;

        UpdateTexts();
    }

    public void start_count()
    {
        if (counting || current_score <= 0)
            return;

        counting = true;

        cat_anim.enabled = true;
        stars.Play();

        particleCounter = 0;
        scoreAccumulator = 0f;

        StartCoroutine(CountRoutine());
    }

    public void end_count()
    {
        cat_anim.speed = 1;
        cat_anim.SetTrigger("Jump");
        screen_anim.SetTrigger("End");
        stars.Stop();
    }

    private IEnumerator CountRoutine()
    {
        int initialScore = current_score;

        while (current_score > 0)
        {
            scoreAccumulator += decrease_speed * Time.deltaTime;

            int amount = Mathf.FloorToInt(scoreAccumulator);

            if (amount <= 0)
            {
                yield return null;
                continue;
            }

            amount = Mathf.Min(amount, current_score);

            scoreAccumulator -= amount;

            current_score -= amount;
            total_score += amount;

            particleCounter += amount;

            while (particleCounter >= score_per_star)
            {
                particleCounter -= score_per_star;

                stars.Emit(1);

                if (starAudio != null && particleSound != null)
                {
                    starAudio.PlayOneShot(particleSound);
                }
            }

            float progress = 1f - ((float)current_score / initialScore);

            star.color = Color.Lerp(
                starOriginalColor,
                star_color_max,
                progress
            );

            Vector3 pos = tongueBasePos;
            pos.y = Mathf.Lerp(
                tongueBasePos.y,
                tongueBasePos.y + (tongue_max_abs),
                progress
            );

            tongue.localPosition = pos;

            cat_anim.speed = Mathf.Lerp(
                anim_speed_range.x,
                anim_speed_range.y,
                progress
            );

            UpdateTexts();

            yield return null;
        }

        star.color = star_color_max;

        Vector3 finalPos = tongueBasePos;
        finalPos.y = tongueBasePos.y +(tongue_max_abs);
        tongue.localPosition = finalPos;

        cat_anim.speed = anim_speed_range.y;

        UpdateTexts();
        end_count();
        counting = false;
    }

    private void UpdateTexts()
    {
        if (score_toadd != null)
            score_toadd.text = current_score.ToString();

        if (score_gained != null)
            score_gained.text = total_score.ToString();

        if (score_gained2 != null)
            score_gained2.text = total_score.ToString();

    }
}