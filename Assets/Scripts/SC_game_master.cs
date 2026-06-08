using UnityEngine;

public class SC_game_master : MonoBehaviour
{
    public static SC_game_master instance;
    public float limits;

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
