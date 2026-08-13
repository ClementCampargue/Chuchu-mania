using UnityEngine;

//#if UNITY_EDITOR

public class SC_debug : MonoBehaviour
{
    private SC_icecream_eat_system eat;

    void Start()
    {
        eat = SC_icecream_eat_system.instance;
    }

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

        if (Input.GetKeyDown(KeyCode.B))
        {
            Time.timeScale = 5;
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            SC_player.instance.Die();
        }
    }
}

//#endif