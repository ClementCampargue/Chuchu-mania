using UnityEngine;

public class SC_cage : MonoBehaviour
{

    private SC_icecream_eat_system system;

    public int Health = 1;
    public SC_juiciness juice;
    public GameObject fire_system;
    public GameObject win_screen;
    void Start()
    {
        system = SC_icecream_eat_system.instance;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && system.isPowerUpActive && Health !=0)
        {
            Debug.Log("die");
            die();
        }
    }

    void die()
    {
        Health--;
        juice.PlayJuice();
        if (Health == 0)
        {
            fire_system.SetActive(false);
            juice.PlayJuice();
        }
        Invoke("win_sc", 1);
    }

    void win_sc()
    {
        Time.timeScale = 0f;

        win_screen.SetActive(true);

    }
}
