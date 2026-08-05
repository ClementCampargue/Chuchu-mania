using UnityEngine;

public class SC_level_intro : MonoBehaviour
{


    private SC_starbit_spawning spawning;



    void Start()
    {
        spawning = SC_starbit_spawning.instance;
        SC_player.instance.canMove = false;
    }


    public void StartGame()
    {
        spawning.Spawn_collectibles();
        SC_player.instance.canMove = true;

    }
}