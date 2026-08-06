using UnityEngine;

public class SC_debug : MonoBehaviour
{

    private SC_icecream_eat_system eat;
    void Start()
    {
        eat = SC_icecream_eat_system.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            SC_score.Instance.score = 1000;
            eat.ActivatePowerUpInstant();

        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            SC_player.instance.Revive();

        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            SC_phases.instance.NextPhase();

        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            SC_player.instance.TakeDamage(1, SC_player.instance.transform.position);

        }
    }
}
