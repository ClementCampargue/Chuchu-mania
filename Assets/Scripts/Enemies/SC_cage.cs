using UnityEngine;

public class SC_cage : MonoBehaviour
{

    private SC_icecream_eat_system system;

    public int Health = 1;
    public GameObject fire_system;
    public GameObject win_screen;
    public SC_juiciness juice_damage;
    public SC_juiciness juice_death;
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
        if (Health == 0)
        {
            SC_icecream_eat_system.instance.loosing_points = false;
            fire_system.SetActive(false);
            juice_death.PlayJuice();
            Invoke("win_sc", 1);
        }
        else
        {
            juice_damage.PlayJuice();

        }
    }

    void win_sc()
    {
        Time.timeScale = 0f;

        win_screen.SetActive(true);

    }
}
