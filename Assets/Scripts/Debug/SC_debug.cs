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
    }
}
