using UnityEngine;

public class SC_damage_player : MonoBehaviour
{

    public int damage = 1;
    public Vector2 ejection_power;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            SC_player.instance.TakeDamage(damage, ejection_power,transform.position);
        }
    }
}
