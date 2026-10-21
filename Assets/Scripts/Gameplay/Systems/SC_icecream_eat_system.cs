using Discord;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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

    // Annulation de la consommation
    private bool cancelEatRequested = false;
    private bool isEatingCoroutineRunning = false;

    public List<SC_icecream_fall> selectedCreams =
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
    public bool caneat = true;
    public bool canfillup = true;

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

        if (!caneat)
            return;

        // -----------------------------------------------------
        // START SELECTION
        // -----------------------------------------------------

        if (eat_input.action.IsPressed() &&
            currrent_ice_cream > 0 &&
            !isPowerUpActive &&
            player.isGrounded &&
            sc_health_system.instance.current_health > 0 &&
            !isEating)
        {
            if (!player.canMove)
                return;

            if (!isSelecting)
            {
                StartCoroutine(SelectionCoroutine());
            }
        }

        // -----------------------------------------------------
        // RELEASE
        // -----------------------------------------------------

        if (!eat_input.action.IsPressed() &&
            isSelecting &&
            player.isGrounded &&
            sc_health_system.instance.current_health > 0 &&
            !isEating)
        {
            isSelecting = false;
            forceEatAll = true;

            StartCoroutine(EatSelectedCreamsCoroutine());
        }

        CheckPhases();
    }

    // =========================================================
    // CANCEL EAT
    // =========================================================

// =========================================================
// CANCEL EAT
// =========================================================

public void CancelEat()
    {
        if (!isEating && !isSelecting)
            return;

        cancelEatRequested = true;
        forceEatAll = false;
        isSelecting = false;

        // Si on annule pendant la sélection, on désélectionne
        // les crèmes et on les remet en mouvement.
        if (!isEating)
        {
            foreach (SC_icecream_fall cream in selectedCreams)
            {
                if (cream == null)
                    continue;

                cream.Deselect();
            }

            selectedCreams.Clear();

            multiplier = 0;
            multiplierText.gameObject.SetActive(false);

            RealignRemainingCreams();

            cancelEatRequested = false;
        }
    }

    // =========================================================
    // REALIGN REMAINING CREAMS
    // =========================================================

    private void RealignRemainingCreams()
    {
        for (int i = 0; i < creams.Count; i++)
        {
            SC_icecream_fall cream = creams[i];

            if (cream == null)
                continue;

            // Réattribuer la position cible selon l'ordre actuel.
            if (i < creams_points.Count)
            {
                cream.currentTargetPosition = creams_points[i];
            }

            // Autoriser la crème à retomber.
            cream.hasLanded = false;
        }

        currrent_ice_cream = creams.Count;
    }
    // =========================================================
    // PHASES
    // =========================================================

    private void CheckPhases()
    {
        if (!phase1Triggered && displayedFill >= 0.33f)
        {
            phase1Triggered = true;
            SC_phases.instance.NextPhase();
        }

        if (!phase2Triggered && displayedFill >= 0.66f)
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
        cancelEatRequested = false;

        multiplier = 0;
        selectedCreams.Clear();

        multiplierText.gameObject.SetActive(true);
        UpdateMultiplierUI();

        int i = creams.Count - 1;

        while (i >= 0 &&
               eat_input.action.IsPressed() &&
               !cancelEatRequested)
        {
            SC_icecream_fall cream = creams[i];

            if (cream == null)
            {
                i--;
                continue;
            }

            if (!cream.hasLanded)
            {
                cream.transform.position = new Vector3(
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

        // Si l'annulation est arrivée pendant la sélection,
        // on termine proprement sans lancer la consommation.
        if (cancelEatRequested && !isEating)
        {
            foreach (SC_icecream_fall cream in selectedCreams)
            {
                if (cream != null)
                    cream.Deselect();
            }

            selectedCreams.Clear();

            multiplier = 0;
            multiplierText.gameObject.SetActive(false);

            isSelecting = false;
            forceEatAll = false;
            cancelEatRequested = false;
        }
    }

    // =========================================================
    // SPAWN
    // =========================================================

    public void get_cream(GameObject cream_type)
    {
        if (player == null)
            return;

        if (Vector3.Distance(
                player.transform.position,
                transform.position) < minDistanceForSpawn)
        {
            return;
        }

        if (creams.Count >= creams_points.Count)
            return;

        SC_icecream_fall cream = Instantiate(
            cream_type,
            creams_points[creams_points.Count - 1].position,
            Quaternion.identity
        ).GetComponent<SC_icecream_fall>();

        cream.currentTargetPosition = creams_points[creams.Count];
        cream.hasLanded = false;

        creams.Add(cream);
        currrent_ice_cream = creams.Count;
    }

    // =========================================================
    // EAT ALL
    // =========================================================

    public void eat_all()
    {
        if (isEatingCoroutineRunning)
            return;

        cancelEatRequested = false;

        selectedCreams = new List<SC_icecream_fall>(creams);

        multiplier = selectedCreams.Count;
        forceEatAll = true;

        StartCoroutine(EatSelectedCreamsCoroutine());
    }

    // =========================================================
    // EAT SELECTED CREAMS
    // =========================================================

    private IEnumerator EatSelectedCreamsCoroutine()
    {
        if (isEatingCoroutineRunning)
            yield break;

        isEatingCoroutineRunning = true;

        // Si canfillup est désactivé, on refuse de manger
        // et on désélectionne les crèmes.
        if (selectedCreams.Count == 10)
        {
            canfillup = true;
        }

        if (!canfillup)
        {
            isEating = true;

            for (int i = selectedCreams.Count - 1; i >= 0; i--)
            {
                if (selectedCreams[i] != null)
                    selectedCreams[i].Deselect();
            }

            yield return new WaitForSeconds(1f);

            isEating = false;
            isSelecting = false;
            forceEatAll = false;
            cancelEatRequested = false;
            isEatingCoroutineRunning = false;

            selectedCreams.Clear();
            multiplierText.gameObject.SetActive(false);

            yield break;
        }

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

        for (int i = selectedCreams.Count - 1; i >= 0; i--)
        {
            // Arrêt avant de commencer la prochaine crème.
            if (cancelEatRequested)
                break;

            if (player == null)
                break;

            StartCoroutine(ScaleBack());

            SC_icecream_fall cream = selectedCreams[i];

            if (cream == null)
                continue;

            eaten_cream++;

            player.moveInput = Vector2.zero;

            cream.Deselect();

            SC_combo_system.Instance.AddToCombo(cream.type);

            // La crème est considérée comme mangée à partir d'ici.
            cream.Eat();

            creams.Remove(cream);

            currrent_ice_cream = creams.Count;

            // -------------------------------------------------
            // STOMACH FILL
            // -------------------------------------------------

            if (canfillup)
            {
                float targetFill =
                    displayedFill + stomach_fill_per_cream;

                if (progressiveEat)
                {
                    while (displayedFill < targetFill - 0.001f)
                    {
                        // Annulation pendant le remplissage :
                        // on conserve le remplissage déjà effectué.
                        if (cancelEatRequested)
                            break;

                        displayedFill = Mathf.MoveTowards(
                            displayedFill,
                            targetFill,
                            eatSpeed * Time.deltaTime
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

                // Si l'annulation a été demandée pendant le
                // remplissage progressif, on ne complète pas
                // artificiellement le remplissage de cette crème.
                if (!cancelEatRequested)
                {
                    displayedFill = targetFill;

                    mat.SetFloat(
                        "_Fill_amount",
                        displayedFill
                    );
                }
            }

            CheckPhases();
            CheckPowerUp();

            // La crème traitée est terminée.
            // On s'arrête avant la suivante si nécessaire.
            if (cancelEatRequested)
                break;

            yield return new WaitForSeconds(delayBetweenCreams);

            if (cancelEatRequested)
                break;
        }

        // -----------------------------------------------------
        // CLEANUP DES CRÈMES NON MANGÉES
        // -----------------------------------------------------

        foreach (SC_icecream_fall cream in selectedCreams)
        {
            if (cream != null && creams.Contains(cream))
            {
                cream.Deselect();
                cream.transform.localScale = Vector3.one;
            }
        }

        RealignRemainingCreams(); currrent_ice_cream = creams.Count;

        // -----------------------------------------------------
        // FIN DE CONSOMMATION
        // -----------------------------------------------------

        FinishEating();

        // Enregistrer uniquement la progression réellement mangée.
        if (eaten_cream > 0 &&
            SceneManager.GetActiveScene().name != "TUTO")
        {
            SC_SaveManager.instance.AddAchievementProgress(
                AchievementType.eat100cream,
                eaten_cream
            );
        }

        selectedCreams.Clear();

        forceEatAll = false;
        isSelecting = false;
        cancelEatRequested = false;
        isEatingCoroutineRunning = false;
    }

    // =========================================================
    // FINISH EATING
    // =========================================================

    private void FinishEating()
    {
        if (player != null)
        {
            player.anim.SetBool("Eat", false);
            player.moveInput = Vector2.zero;
        }

        multiplierText.gameObject.SetActive(false);

        eating_sfx.Stop();

        calculate_score();

        isEating = false;
    }

    // =========================================================
    // POWERUP
    // =========================================================

    private void CheckPowerUp()
    {
        if (displayedFill >= 1f && !isPowerUpActive)
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
            SC_music_manager.instance.update_music(music);

            player.powerup();

            isPowerUpActive = true;
        }
    }

    // =========================================================
    // UI
    // =========================================================

    private void UpdateMultiplierUI()
    {
        multiplierText.text = "x" + multiplier.ToString();
    }

    private IEnumerator ScaleBack()
    {
        yield return new WaitForSeconds(0.1f);

        multiplierText.transform.localScale = Vector3.one;
    }

    // =========================================================
    // DAMAGE
    // =========================================================

    public void take_damage()
    {
        // Une prise de dégâts interrompt la consommation.
        CancelEat();

        if (creams.Count == 0)
            return;

        int countToAffect = Mathf.Max(
            1,
            creams.Count / 2
        );

        List<SC_icecream_fall> creamsToAffect =
            creams.GetRange(
                creams.Count - countToAffect,
                countToAffect
            );

        foreach (SC_icecream_fall cream in creamsToAffect)
        {
            if (cream == null)
                continue;

            cream.BounceAndBlink();

            creams.Remove(cream);
        }

        currrent_ice_cream = creams.Count;
    }

    // =========================================================
    // SCORE
    // =========================================================

    private void calculate_score()
    {
        int score = (int)(
            10 *
            eaten_cream *
            eaten_cream
        );

        SC_score.Instance.AddScore(score);

        if (eaten_cream == 10)
        {
            sc_health_system.instance.heal();
        }

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

        displayedFill = Mathf.Clamp01(displayedFill);

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

        cancelEatRequested = false;
        isEatingCoroutineRunning = false;

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

        multiplierText.transform.localScale = Vector3.one;

        eating_sfx.Stop();

        if (player != null)
        {
            player.anim.SetBool("Eat", false);
            player.moveInput = Vector2.zero;
        }
    }

    // =========================================================
    // SPAWN MAX ICE CREAMS
    // =========================================================

    public void SpawnMaxIceCreams(GameObject cream_type)
    {
        Debug.Log("spawned;");

        if (cream_type == null)
            return;

        while (creams.Count < creams_points.Count)
        {
            int previousCount = creams.Count;

            SC_icecream_fall cream = Instantiate(
                cream_type,
                creams_points[creams_points.Count - 1].position,
                Quaternion.identity
            ).GetComponent<SC_icecream_fall>();

            if (cream == null)
                break;

            cream.currentTargetPosition = creams_points[creams.Count];
            cream.hasLanded = false;

            creams.Add(cream);

            currrent_ice_cream = creams.Count;

            if (creams.Count == previousCount)
                break;
        }
    }

public void destroy_all_cream()
    {
        // Annuler toute consommation ou sélection en cours.
        StopAllCoroutines();

        cancelEatRequested = false;
        isEatingCoroutineRunning = false;
        isSelecting = false;
        forceEatAll = false;
        isEating = false;

        // Arrêter l'animation et le son.
        if (player != null)
        {
            player.anim.SetBool("Eat", false);
            player.moveInput = Vector2.zero;
        }

        if (eating_sfx != null)
            eating_sfx.Stop();

        if (multiplierText != null)
            multiplierText.gameObject.SetActive(false);

        // Détruire toutes les crèmes restantes.
        foreach (SC_icecream_fall cream in creams)
        {
            if (cream == null)
                continue;

            // Même comportement visuel que lors d'une consommation.
            cream.Eat();
        }

        // Vider les listes et remettre les compteurs à zéro.
        creams.Clear();
        selectedCreams.Clear();

        currrent_ice_cream = 0;
        multiplier = 0;
        eaten_cream = 0;
    }
}
