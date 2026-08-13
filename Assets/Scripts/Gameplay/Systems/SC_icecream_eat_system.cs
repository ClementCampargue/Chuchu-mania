using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SC_icecream_eat_system : MonoBehaviour
{
    [Header("Multiplier UI")]
    public TextMeshPro multiplierText;
    private int multiplier = 0;

    [Header("Eat Settings")]
    public bool progressiveEat = false;
    public float eatSpeed = 0.5f;

    public bool isEating = false;

    private bool isSelecting = false;
    private bool forceEatAll = false;

    private List<SC_icecream_fall> selectedCreams =
        new List<SC_icecream_fall>();

    [Header("Ice Cream")]
    public List<Transform> creams_points =
        new List<Transform>();

    public List<SC_icecream_fall> creams =
        new List<SC_icecream_fall>();

    public int currrent_ice_cream;

    public InputActionReference eat_input;

    [Header("Fill System")]
    public Material mat;

    private float displayedFill = 0f;

    public float stomach_fill_per_cream = 0.2f;
    public float stomach_fill_speed = 5f;
    public float delayBetweenCreams = 0.1f;

    [Header("Spawn")]
    public float minDistanceForSpawn = 2f;

    [Header("Power Up")]
    public bool isPowerUpActive = false;

    [Header("Phase System")]
    private bool phase1Triggered = false;
    private bool phase2Triggered = false;

    public static SC_icecream_eat_system instance;

    private SC_player player;

    private int eaten_cream;

    public SC_juiciness eat;
    public AudioSource eating_sfx;

    public AudioClip music;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        instance = this;
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        player = SC_player.instance;

        multiplierText.gameObject.SetActive(false);

        multiplier = 0;

        mat.SetFloat("_Fill_amount", 0f);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (player == null)
            return;

        if (!player.enabled)
            return;

        // -----------------------------------------------------
        // START SELECTION
        // -----------------------------------------------------

        if (eat_input.action.IsPressed() &&
            currrent_ice_cream > 0 &&
            !isPowerUpActive &&
            player.isGrounded &&
            !isEating)
        {
            if (!player.canMove)
                return;

            if (sc_health_system.instance.current_health == 0)
                return;

            if (!isSelecting)
            {
                StartCoroutine(
                    SelectionCoroutine()
                );
            }
        }

        // -----------------------------------------------------
        // RELEASE
        // -----------------------------------------------------

        if (eat_input.action.WasReleasedThisFrame() &&
            isSelecting &&
            !isEating)
        {
            /*
             * IMPORTANT :
             *
             * On ne fait PLUS :
             *
             * player.rb.linearVelocity = Vector2.zero;
             *
             * Sinon un tapis roulant / knockback / force
             * externe est immédiatement supprimé.
             */

            isSelecting = false;
            forceEatAll = true;

            StartCoroutine(
                EatSelectedCreamsCoroutine()
            );
        }

        CheckPhases();
    }

    // =========================================================
    // PHASES
    // =========================================================

    private void CheckPhases()
    {
        if (!phase1Triggered &&
            displayedFill >= 0.33f)
        {
            phase1Triggered = true;

            SC_phases.instance.NextPhase();
        }

        if (!phase2Triggered &&
            displayedFill >= 0.66f)
        {
            phase2Triggered = true;

            SC_phases.instance.NextPhase();
        }
    }

    // =========================================================
    // SELECTION
    // =========================================================

    private IEnumerator SelectionCoroutine()
    {
        isSelecting = true;

        multiplier = 0;

        selectedCreams.Clear();

        multiplierText.gameObject.SetActive(true);

        UpdateMultiplierUI();

        int i = creams.Count - 1;

        while (i >= 0 &&
               eat_input.action.IsPressed())
        {
            SC_icecream_fall cream =
                creams[i];

            if (!cream.hasLanded)
            {
                cream.transform.position =
                    new Vector3(
                        cream.transform.position.x,
                        cream.currentTargetPosition.position.y,
                        cream.transform.position.z
                    );

                cream.hasLanded = true;
            }

            cream.Select();

            selectedCreams.Add(cream);

            multiplier++;

            UpdateMultiplierUI();

            yield return new WaitForSeconds(0.08f);

            i--;
        }
    }

    // =========================================================
    // SPAWN
    // =========================================================

    public void get_cream(GameObject cream_type)
    {
        if (Vector3.Distance(
                player.transform.position,
                transform.position) <
            minDistanceForSpawn)
        {
            return;
        }

        if (creams.Count >= creams_points.Count)
            return;

        SC_icecream_fall cream =
            Instantiate(
                cream_type,
                creams_points[
                    creams_points.Count - 1
                ].position,
                Quaternion.identity
            ).GetComponent<SC_icecream_fall>();

        cream.currentTargetPosition =
            creams_points[creams.Count];

        cream.hasLanded = false;

        creams.Add(cream);

        currrent_ice_cream =
            creams.Count;
    }

    // =========================================================
    // EAT ALL
    // =========================================================

    public void eat_all()
    {
        selectedCreams =
            new List<SC_icecream_fall>(creams);

        multiplier =
            selectedCreams.Count;

        forceEatAll = true;

        StartCoroutine(
            EatSelectedCreamsCoroutine()
        );
    }

    // =========================================================
    // EAT
    // =========================================================

    private IEnumerator EatSelectedCreamsCoroutine()
    {
        /*
         * On bloque le contrôle du joueur.
         *
         * MAIS :
         * - pas de velocity = 0
         * - pas de Rigidbody Kinematic
         * - pas de freeze physique
         *
         * SC_player continue donc à appliquer :
         * - tapis roulant
         * - knockback
         * - bounce
         * - forces externes
         */

        player.moveInput = Vector2.zero;

        SC_combo_system.Instance.ResetCombo();

        eaten_cream = 0;

        multiplierText.gameObject.SetActive(true);

        UpdateMultiplierUI();

        isEating = true;

        eating_sfx.Play();

        player.anim.SetBool("Eat", true);

        // -----------------------------------------------------
        // EAT EACH CREAM
        // -----------------------------------------------------

        for (int i =
            selectedCreams.Count - 1;
            i >= 0;
            i--)
        {
            StartCoroutine(ScaleBack());

            eaten_cream++;

            player.moveInput =
                Vector2.zero;

            SC_icecream_fall cream =
                selectedCreams[i];

            cream.Deselect();

            SC_combo_system.Instance.AddToCombo(
                cream.type
            );

            cream.Eat();

            creams.Remove(cream);

            currrent_ice_cream--;

            float targetFill =
                displayedFill +
                stomach_fill_per_cream;

            // -------------------------------------------------
            // PROGRESSIVE
            // -------------------------------------------------

            if (progressiveEat)
            {
                while (displayedFill <
                       targetFill - 0.001f)
                {
                    if (!eat_input.action.IsPressed() &&
                        !forceEatAll)
                    {
                        FinishEating();

                        yield break;
                    }

                    displayedFill =
                        Mathf.MoveTowards(
                            displayedFill,
                            targetFill,
                            eatSpeed *
                            Time.deltaTime
                        );

                    mat.SetFloat(
                        "_Fill_amount",
                        displayedFill
                    );

                    CheckPhases();

                    CheckPowerUp();

                    yield return null;
                }
            }

            // -------------------------------------------------
            // FILL DIRECT
            // -------------------------------------------------

            displayedFill =
                targetFill;

            mat.SetFloat(
                "_Fill_amount",
                displayedFill
            );

            CheckPhases();

            CheckPowerUp();

            yield return new WaitForSeconds(
                delayBetweenCreams
            );
        }

        // -----------------------------------------------------
        // REALIGN REMAINING CREAMS
        // -----------------------------------------------------

        foreach (var c in creams)
        {
            if (c != null)
                c.transform.localScale =
                    Vector3.one;
        }

        for (int j = 0;
             j < creams.Count;
             j++)
        {
            creams[j].currentTargetPosition =
                creams_points[j];
        }

        FinishEating();

        selectedCreams.Clear();

        forceEatAll = false;
        isSelecting = false;
    }

    // =========================================================
    // FINISH EATING
    // =========================================================

    private void FinishEating()
    {
        player.anim.SetBool("Eat", false);

        multiplierText.gameObject.SetActive(false);

        eating_sfx.Stop();

        calculate_score();

        isEating = false;

        player.moveInput = Vector2.zero;
    }

    // =========================================================
    // POWERUP
    // =========================================================

    private void CheckPowerUp()
    {
        if (displayedFill >= 1f &&
            !isPowerUpActive)
        {
            ActivatePowerUpInstant();
        }
    }

    public void ActivatePowerUpInstant()
    {
        displayedFill = 1f;

        mat.SetFloat(
            "_Fill_amount",
            displayedFill
        );

        CheckPhases();

        if (!isPowerUpActive)
        {
            SC_music_manager.instance
                .update_music(music);

            player.powerup();

            isPowerUpActive = true;
        }
    }

    // =========================================================
    // UI
    // =========================================================

    private void UpdateMultiplierUI()
    {
        multiplierText.text =
            "x" + multiplier.ToString();
    }

    private IEnumerator ScaleBack()
    {
        yield return new WaitForSeconds(0.1f);

        multiplierText.transform.localScale =
            Vector3.one;
    }

    // =========================================================
    // DAMAGE
    // =========================================================

    public void take_damage()
    {
        if (creams.Count == 0)
            return;

        int countToAffect =
            Mathf.Max(
                1,
                creams.Count / 2
            );

        List<SC_icecream_fall> creamsToAffect =
            creams.GetRange(
                creams.Count -
                countToAffect,
                countToAffect
            );

        foreach (var cream in creamsToAffect)
        {
            cream.BounceAndBlink();

            creams.Remove(cream);
        }

        currrent_ice_cream =
            creams.Count;
    }

    // =========================================================
    // SCORE
    // =========================================================

    private void calculate_score()
    {
        int score =
            (int)(
                10 *
                eaten_cream *
                eaten_cream
            );

        SC_score.Instance.AddScore(score);

        Debug.Log(
            "Crèmes mangées : " +
            eaten_cream +
            " | Score : " +
            score
        );
    }

    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("DEBUG - Fill Stomach")]
    public void DebugFillStomach()
    {
        displayedFill += 0.25f;

        displayedFill =
            Mathf.Clamp01(displayedFill);

        mat.SetFloat(
            "_Fill_amount",
            displayedFill
        );

        CheckPhases();

        Debug.Log(
            "DEBUG: Stomach filled to " +
            displayedFill
        );
    }

    // =========================================================
    // RESET
    // =========================================================

    public void ResetSystem()
    {
        StopAllCoroutines();

        foreach (SC_icecream_fall cream in creams)
        {
            if (cream != null)
                Destroy(cream.gameObject);
        }

        creams.Clear();
        selectedCreams.Clear();

        currrent_ice_cream = 0;

        multiplier = 0;
        eaten_cream = 0;

        isEating = false;
        isSelecting = false;
        forceEatAll = false;
        isPowerUpActive = false;

        phase1Triggered = false;
        phase2Triggered = false;

        displayedFill = 0f;

        mat.SetFloat(
            "_Fill_amount",
            0f
        );

        multiplierText.text = "x0";

        multiplierText.gameObject.SetActive(false);

        multiplierText.transform.localScale =
            Vector3.one;

        eating_sfx.Stop();

        if (player != null)
            player.moveInput = Vector2.zero;
    }
}