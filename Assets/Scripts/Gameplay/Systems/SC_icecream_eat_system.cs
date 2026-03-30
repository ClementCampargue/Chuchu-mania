using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SC_icecream_eat_system : MonoBehaviour
{
    public List<Transform> creams_points = new List<Transform>();
    public List<SC_icecream_fall> creams = new List<SC_icecream_fall>();
    public int currrent_ice_cream;
    public InputActionReference eat_input;

    public static SC_icecream_eat_system instance;
    public Material mat;
    private float displayedFill = 0f;
    public float stomach_fill_per_cream;
    public float stomach_fill_speed;
    public float delayBetweenCreams = 0.1f; // délai fixe entre chaque glace

    public float minDistanceForSpawn = 2f; // distance minimale pour manger une glace
    public Transform player; // assigner le joueur dans l'inspecteur ou via code
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        mat.SetFloat("_Fill_amount", 0);

    }

    // Update is called once per frame
    void Update()
    { 
        if (eat_input.action.WasPressedThisFrame() && currrent_ice_cream>0)
        {
            eat_all();
        }

    }

    public void get_cream(GameObject cream_type)
    {
        // Vérifie la distance avant de spawn
        if (Vector3.Distance(player.position, transform.position) < minDistanceForSpawn)
        {
            // Trop proche du joueur, ne pas spawn
            return;
        }

        SC_icecream_fall cream = Instantiate(cream_type, transform.position, Quaternion.identity).GetComponent<SC_icecream_fall>();
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
        for (int i = creams.Count - 1; i >= 0; i--)
        {
            SC_icecream_fall cream = creams[i];
            cream.Eat();

            // Nouvelle cible pour cette crème
            float targetFill = displayedFill + stomach_fill_per_cream;

            // Lerp progressif vers la cible, mais indépendant du délai
            while (displayedFill < targetFill - 0.001f)
            {
                displayedFill = Mathf.Lerp(displayedFill, targetFill, Time.deltaTime * stomach_fill_speed);
                mat.SetFloat("_Fill_amount", displayedFill);
                yield return null;
            }

            // Assure que la valeur atteint exactement la cible
            displayedFill = targetFill;
            mat.SetFloat("_Fill_amount", displayedFill);

            // Attend le délai fixe avant la prochaine glace
            yield return new WaitForSeconds(delayBetweenCreams);
        }

        creams.Clear();
        currrent_ice_cream = 0;
    }
    void calculate_score()
    {
        SC_score.Instance.AddScore(100 * currrent_ice_cream);
    }

    public void take_damage()
    {
        int countToAffect = creams.Count / 2;
        List<SC_icecream_fall> creamsToAffect = creams.GetRange(creams.Count - countToAffect, countToAffect);

        foreach (var cream in creamsToAffect)
        {
            cream.BounceAndBlink();
            creams.Remove(cream); // safe ici car on ne boucle pas directement sur creams
        }

        currrent_ice_cream = currrent_ice_cream / 2;
    }
}
