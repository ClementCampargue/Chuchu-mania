using UnityEngine;
using UnityEngine.SceneManagement;

public class SC_debug_spawn_game_master : MonoBehaviour
{
    public GameObject gamemaster;
    public Vector3 vector;
    public bool reload_ =true;
    void Start()
    {
        if(SC_game_master.instance == null)
        {
           Transform trs = Instantiate(gamemaster).transform;
            trs.position = vector;
            Invoke("delay", 0.1f);
        }
    }
    void delay()
    {if (!reload_) return;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
