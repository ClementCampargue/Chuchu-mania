using UnityEngine;

public class SC_dard : MonoBehaviour
{
    public SC_damage_player damae;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (SC_balloon_player.instance.isActive)
        {
            damae.enabled = true;
        }
        else
        {
            damae.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (SC_player.instance.canTakeDamage && !SC_player.instance.isInvincible)
            {
                if (SC_balloon_player.instance.isActive)
                {
                }
                else
                {
                    SC_balloon_player.instance.Activate();

                    Destroy(gameObject);
                }
            }
   
        }
    }
}
