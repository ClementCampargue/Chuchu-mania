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
    [Tooltip("Déplacement maximal de la langue sur Y")]
    public float tongue_max_abs = 0.5f;

    [Tooltip("Montant nécessaire pour atteindre le déplacement maximal")]
    public float money_for_max_tongue = 100f;

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
        foreach (SO_level level in SC_game_master.instance.world.levels)
        {
            SC_SaveManager.instance.SetLevelUnlocked(level, true);
        }

        total_money.text = SC_SaveManager.instance.data.money.ToString();

        total_score = PlayerPrefs.GetInt("Score");
        total_money_ = SC_SaveManager.instance.data.money;

        score_total.text = total_score.ToString("D6");

        tongueBasePos = tongue.localPosition;

        money_won = score_for_one_coin > 0
            ? total_score / score_for_one_coin
            : 0;

        starOriginalColor = star.color;

        cat_anim.speed = anim_speed_range.x;
        cat_anim.enabled = false;

        SC_SaveManager.instance.Addmoney(money_won);

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
        cat_anim.speed = 1f;
        cat_anim.SetTrigger("Jump");
        screen_anim.SetTrigger("End");
        stars.Stop();
    }

    private IEnumerator CountRoutine()
    {
        int initialMoney = money_won;

        // La langue descend selon le montant total gagné.
        float tongueAmount = money_for_max_tongue > 0f
            ? Mathf.Clamp01((float)initialMoney / money_for_max_tongue)
            : 1f;

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

            // Émission des particules selon l'argent transféré.
            particleCounter += amount;

            while (particleCounter >= score_per_star && score_per_star > 0)
            {
                particleCounter -= score_per_star;

                stars.Emit(1);

                if (starAudio != null && particleSound != null)
                {
                    starAudio.PlayOneShot(particleSound);
                }
            }

            // Progression du comptage.
            float progress = 1f - ((float)money_won / initialMoney);

            // La couleur de l'étoile évolue progressivement.
            star.color = Color.Lerp(
                starOriginalColor,
                star_color_max,
                progress
            );

            // La langue descend en fonction de l'argent gagné
            // ET de la progression du comptage.
            Vector3 pos = tongueBasePos;

            pos.y = Mathf.Lerp(
                tongueBasePos.y,
                tongueBasePos.y + tongue_max_abs * tongueAmount,
                progress
            );

            tongue.localPosition = pos;

            // La vitesse de l'animation évolue progressivement.
            cat_anim.speed = Mathf.Lerp(
                anim_speed_range.x,
                anim_speed_range.y,
                progress
            );

            UpdateTexts();

            yield return null;
        }

        // Fin du comptage.
        stars.Stop();
        stars.Clear();

        star.color = star_color_max;

        Vector3 finalPos = tongueBasePos;
        finalPos.y = tongueBasePos.y + tongue_max_abs * tongueAmount;

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
        skip.action.Enable();
        canquit = true;
    }

    public void backtohub()
    {
        SC_screenshot_transition.instance.Capture("HUB");
    }
}
