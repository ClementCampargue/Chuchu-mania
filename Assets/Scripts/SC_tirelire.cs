using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

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
    public int score_for_one_coin;
    public int total_score;
    public int total_money_;
    public int money_won;

    [Header("Vitesse d'incrémentation")]
    [Tooltip("Vitesse minimale de transfert des points par seconde")]
    public float increase_speed_min = 50f;

    [Tooltip("Vitesse maximale de transfert des points par seconde")]
    public float increase_speed_max = 100f;

    private float increase_speed;

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
    public TextMeshPro score_total;
    public TextMeshPro score_toadd;
    public TextMeshPro total_money;

    private bool canquit;
    private bool counting;

    private Vector3 tongueBasePos;
    private int particleCounter;
    private float scoreAccumulator;

    private Color starOriginalColor;

    public InputActionReference skip;

    private void Start()
    {
        Debug.Log("MONEYY  " + PlayerPrefs.GetInt("Score"));

        total_money.text = SC_money_manager.instance.money.ToString();

        total_score = PlayerPrefs.GetInt("Score");
        total_money_ = SC_money_manager.instance.money;

        score_total.text = total_score.ToString("D6");

        tongueBasePos = tongue.localPosition;

        money_won = (int)(total_score / score_for_one_coin);

        starOriginalColor = star.color;

        cat_anim.speed = anim_speed_range.x;
        cat_anim.enabled = false;

        PlayerPrefs.SetInt("Money", total_money_ + money_won);

        UpdateTexts();
    }

    private void Update()
    {
        if (skip.action.WasPerformedThisFrame())
        {
            if (canquit)
            {
                backtohub();
            }
        }
    }

    public void start_count()
    {
        if (counting || money_won <= 0)
            return;

        counting = true;

        // Choisit une vitesse aléatoire dans la range
        increase_speed = Random.Range(
            increase_speed_min,
            increase_speed_max
        );

        Debug.Log("Vitesse de comptage : " + increase_speed);

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
        int initialScore = money_won;

        // On s'assure que les particules sont arrêtées au début
        stars.Stop();
        stars.Clear();

        while (money_won > 0)
        {
            scoreAccumulator += increase_speed * Time.deltaTime;

            int amount = Mathf.FloorToInt(scoreAccumulator);

            if (amount <= 0)
            {
                yield return null;
                continue;
            }

            amount = Mathf.Min(amount, money_won);

            scoreAccumulator -= amount;

            money_won -= amount;
            total_money_ += amount;

            // Particules pendant l'incrémentation
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

            float progress = 1f - ((float)money_won / initialScore);

            star.color = Color.Lerp(
                starOriginalColor,
                star_color_max,
                progress
            );

            Vector3 pos = tongueBasePos;

            pos.y = Mathf.Lerp(
                tongueBasePos.y,
                tongueBasePos.y + tongue_max_abs,
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

        // Fin du comptage : arrêt immédiat des particules
        stars.Stop();
        stars.Clear();

        star.color = star_color_max;

        Vector3 finalPos = tongueBasePos;
        finalPos.y = tongueBasePos.y + tongue_max_abs;

        tongue.localPosition = finalPos;

        cat_anim.speed = anim_speed_range.y;

        UpdateTexts();

        end_count();
        counting = false;
    }

    private void UpdateTexts()
    {
        if (score_toadd != null)
            score_toadd.text = money_won.ToString();

        if (total_money != null)
            total_money.text = total_money_.ToString();
    }

    public void can_quit()
    {
        canquit = true;
    }

    public void backtohub()
    {
        SC_screenshot_transition.instance.Capture("HUB");
    }
}
