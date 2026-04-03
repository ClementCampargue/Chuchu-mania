using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SC_icecream_eat_system : MonoBehaviour
{
    [Header("Eat Settings")]
    public bool progressiveEat = false; // Si vrai, le joueur mange progressivement
    public float eatSpeed = 0.5f;       // Vitesse de manger progressive

    private bool isEating = false;      // Empêche de bouger pendant l'action
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
    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        player = SC_player.instance;
        mat.SetFloat("_Fill_amount", 0);
    }
    void Update()
    {
        if (eat_input.action.WasPressedThisFrame() && currrent_ice_cream > 0 && !isPowerUpActive && player.isGrounded && !isEating)
        {
            StartCoroutine(EatCreamsCoroutine());
        }
    }
    public void get_cream(GameObject cream_type)
    {
        if (Vector3.Distance(player.transform.position, transform.position) < minDistanceForSpawn)
            return;

        SC_icecream_fall cream = Instantiate(cream_type, transform.position, Quaternion.identity)
            .GetComponent<SC_icecream_fall>();

        cream.targetPosition = creams_points[currrent_ice_cream];

        creams.Add(cream);
        currrent_ice_cream++;
    }
    public void eat_all()
    {
        calculate_score();
        StartCoroutine(EatCreamsCoroutine());
    }

    private IEnumerator EatCreamsCoroutine()
    {
        isEating = true;
        player.canMove = false;

        calculate_score();

        int i = creams.Count - 1;
        while (i >= 0)
        {
            SC_icecream_fall cream = creams[i];
            cream.Eat();
            creams.RemoveAt(i);
            currrent_ice_cream--;

            float targetFill = displayedFill + stomach_fill_per_cream;

            if (progressiveEat)
            {
                while (displayedFill < targetFill - 0.001f)
                {
                    if (!eat_input.action.IsPressed())
                    {
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

            i--;
        }
        creams.Clear();
        currrent_ice_cream = 0;

        isEating = false;
        player.canMove = true;
    }
    private IEnumerator PowerUpCoroutine()
    {
        player.canMove = true;

        isPowerUpActive = true;
        Debug.Log("POWER UP ACTIVÉ");

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
        int countToAffect = creams.Count / 2;

        List<SC_icecream_fall> creamsToAffect =
            creams.GetRange(creams.Count - countToAffect, countToAffect);

        foreach (var cream in creamsToAffect)
        {
            cream.BounceAndBlink();
            creams.Remove(cream);
        }

        currrent_ice_cream = currrent_ice_cream / 2;
    }

    void calculate_score()
    {
        SC_score.Instance.AddScore(100 * currrent_ice_cream);
    }
    public void ActivatePowerUpInstant()
    {
        // Met le fill au maximum
        displayedFill = 1f;
        mat.SetFloat("_Fill_amount", displayedFill);

        // Si le PowerUp n'est pas déjà actif, lance la coroutine
        if (!isPowerUpActive)
        {
            player.powerup();
            StartCoroutine(PowerUpCoroutine());
        }
    }
}