using UnityEngine;

public class SC_game_master : MonoBehaviour
{
    public static SC_game_master instance;
    public string previousScene;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Application.targetFrameRate = 120;
        DontDestroyOnLoad(gameObject);
    }

}