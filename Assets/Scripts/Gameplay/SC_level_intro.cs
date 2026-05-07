using UnityEngine;

public class SC_level_intro : MonoBehaviour
{

    public static bool gameStarted = false;

    public SC_starbit_spawning spawning;

    void Start()
    {
        gameStarted = false;
    }


    public void StartGame()
    {
        spawning.Spawn_collectibles();
        gameStarted = true;

    }
}