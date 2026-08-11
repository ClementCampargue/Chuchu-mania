using UnityEngine;

public class SC_game_master : MonoBehaviour
{
    public static SC_game_master instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);
    }

}