using UnityEngine;

public class SC_starbit : MonoBehaviour
{
    [HideInInspector]
    public SC_starbit_spawning spawner;

    public float detectionRadius = 0.5f;
    public LayerMask playerLayer;
    public GameObject ice_cream_UI;
    private SC_icecream_eat_system eat_system;

    private void Start()
    {
        eat_system = SC_icecream_eat_system.instance;
    }
    private void Update()
    {
        // Vérifie si un collider du joueur est dans la zone
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRadius, playerLayer);

        if (hit != null)
        {
            Collect();
        }
    }

    void Collect()
    {
        if (eat_system.currrent_ice_cream < 10)
        {
            spawner.OnCollectiblePicked(gameObject);
            eat_system.get_cream(ice_cream_UI);
            Destroy(gameObject);
        }
    }

    // Pour visualiser la zone dans l'éditeur
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}