using UnityEngine;

public class SC_level_intro : MonoBehaviour
{

    public static bool gameStarted = false;

    private SC_starbit_spawning spawning;

    private void Awake()
    {
        if (!gameObject.activeInHierarchy)
        {
            gameStarted = true;
        }
    }

    void Start()
    {
        spawning = SC_starbit_spawning.instance;
        gameStarted = false;
    }


    public void StartGame()
    {
        spawning.Spawn_collectibles();
        gameStarted = true;

    }
}