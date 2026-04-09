using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class SC_icecream_eat_system : MonoBehaviour
{
    [Header("Multiplier UI")]
    public TextMeshPro multiplierText;
    private int multiplier = 0;

    [Header("Eat Settings")]
    public bool progressiveEat = false;
    public float eatSpeed = 0.5f;

    private bool isEating = false;

    private bool isSelecting = false;
    private bool forceEatAll = false;
    private List<SC_icecream_fall> selectedCreams = new List<SC_icecream_fall>();

    [Header("Ice Cream")]
    public List<Transform> creams_points = new List<Transform>();
    public List<SC_icecream_fall> creams = new List<SC_icecream_fall>();
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
    public float drainSpeed = 0.25f;

    public static SC_icecream_eat_system instance;
    private SC_player player;
    private int eaten_cream;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        multiplierText.gameObject.SetActive(false);
        multiplier = 0;
        player = SC_player.instance;
        mat.SetFloat("_Fill_amount", 0);
    }

    void Update()
    {
        if (eat_input.action.IsPressed() && currrent_ice_cream > 0 && !isPowerUpActive && player.isGrounded && !isEating)
        {
            if (!isSelecting)
                StartCoroutine(SelectionCoroutine());
        }

        if (eat_input.action.WasReleasedThisFrame() && isSelecting && !isEating)
        {
            isSelecting = false;
            forceEatAll = true;

            StartCoroutine(EatSelectedCreamsCoroutine());
        }
    }

    IEnumerator SelectionCoroutine()
    {
        isSelecting = true;
        multiplier = 0;
        selectedCreams.Clear();

        multiplierText.gameObject.SetActive(true);
        UpdateMultiplierUI();

        int i = creams.Count - 1;

        while (i >= 0 && eat_input.action.IsPressed())
        {
            SC_icecream_fall cream = creams[i];

            if (!cream.hasLanded)
            {
                // Si la glace est en chute, on force sa position avant de la sélectionner
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
    }

    public void get_cream(GameObject cream_type)
    {
        if (Vector3.Distance(player.transform.position, transform.position) < minDistanceForSpawn)
            return;

        if (creams.Count >= creams_points.Count)
            return;

        SC_icecream_fall cream = Instantiate(cream_type, transform.position, Quaternion.identity)
            .GetComponent<SC_icecream_fall>();

        cream.currentTargetPosition = creams_points[creams.Count];
        cream.hasLanded = false;

        creams.Add(cream);
        currrent_ice_cream = creams.Count;
    }

    public void eat_all()
    {
        selectedCreams = new List<SC_icecream_fall>(creams);
        multiplier = selectedCreams.Count;

        forceEatAll = true;
        StartCoroutine(EatSelectedCreamsCoroutine());
    }

    private IEnumerator EatSelectedCreamsCoroutine()
    {
        SC_combo_system.Instance.ResetCombo();
        eaten_cream = 0;

        multiplierText.gameObject.SetActive(true);
        UpdateMultiplierUI();

        isEating = true;
        player.canMove = false;
        player.anim_.SetBool("Eat", true);

        for (int i = selectedCreams.Count - 1; i >= 0; i--)
        {
            StartCoroutine(ScaleBack());

            eaten_cream++;

            SC_icecream_fall cream = selectedCreams[i];
            cream.Deselect();

            SC_combo_system.Instance.AddToCombo(cream.type);
            cream.Eat();

            creams.Remove(cream);
            currrent_ice_cream--;

            float targetFill = displayedFill + stomach_fill_per_cream;

            if (progressiveEat)
            {
                while (displayedFill < targetFill - 0.001f)
                {
                    if (!eat_input.action.IsPressed() && !forceEatAll)
                    {
                        calculate_score();

                        player.anim_.SetBool("Eat", false);
                        multiplierText.gameObject.SetActive(false);
                        isEating = false;
                        player.canMove = true;
                        yield break;
                    }

                    displayedFill = Mathf.MoveTowards(displayedFill, targetFill, eatSpeed * Time.deltaTime);
                    mat.SetFloat("_Fill_amount", displayedFill);

                    if (displayedFill >= 1f && !isPowerUpActive)
                    {
                        player.powerup();
                        StartCoroutine(PowerUpCoroutine());
                    }

                    yield return null;
                }
            }

            displayedFill = targetFill;
            mat.SetFloat("_Fill_amount", displayedFill);

            yield return new WaitForSeconds(delayBetweenCreams);
        }

        // Reset visuel des restantes
        foreach (var c in creams)
            c.transform.localScale = Vector3.one;

        // Réassigner les positions
        for (int j = 0; j < creams.Count; j++)
        {
            creams[j].currentTargetPosition = creams_points[j];
        }

        player.anim_.SetBool("Eat", false);
        multiplierText.gameObject.SetActive(false);

        calculate_score();

        isEating = false;
        player.canMove = true;

        selectedCreams.Clear();
        forceEatAll = false;
        isSelecting = false;
    }

    void UpdateMultiplierUI()
    {
        multiplierText.text = "x" + multiplier.ToString();
    }

    IEnumerator ScaleBack()
    {
        yield return new WaitForSeconds(0.1f);
        multiplierText.transform.localScale = Vector3.one;
    }

    private IEnumerator PowerUpCoroutine()
    {
        player.canMove = true;

        isPowerUpActive = true;

        while (displayedFill > 0f)
        {
            displayedFill -= drainSpeed * Time.deltaTime;
            displayedFill = Mathf.Clamp01(displayedFill);

            mat.SetFloat("_Fill_amount", displayedFill);

            yield return null;
        }

        displayedFill = 0f;
        mat.SetFloat("_Fill_amount", displayedFill);
        player.end_powerup();

        isPowerUpActive = false;
    }

    public void take_damage()
    {
        if (creams.Count == 0) return;

        int countToAffect = Mathf.Max(1, creams.Count / 2);

        List<SC_icecream_fall> creamsToAffect =
            creams.GetRange(creams.Count - countToAffect, countToAffect);

        foreach (var cream in creamsToAffect)
        {
            cream.BounceAndBlink();
            creams.Remove(cream);
        }

        currrent_ice_cream = creams.Count;
    }

    void calculate_score()
    {
        Debug.Log("calculated");
        SC_score.Instance.AddScore((int)(100 * eaten_cream * multiplier * SC_point_boost.Instance.boostMultiplier));
    }

    public void ActivatePowerUpInstant()
    {
        displayedFill = 1f;
        mat.SetFloat("_Fill_amount", displayedFill);

        if (!isPowerUpActive)
        {
            player.powerup();
            StartCoroutine(PowerUpCoroutine());
        }
    }
}