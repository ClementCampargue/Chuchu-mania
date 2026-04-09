using UnityEngine;

public class SC_starbit : MonoBehaviour
{
    [HideInInspector]
    public SC_starbit_spawning spawner;

    public float detectionRadius = 0.5f;
    public LayerMask playerLayer;
    public GameObject ice_cream_UI;

    private SC_icecream_eat_system eat_system;

    private bool isAttracted = false;
    private Transform attractTarget;
    private float attractSpeed;

    private void Start()
    {
        eat_system = SC_icecream_eat_system.instance;
    }

    private void Update()
    {
        if (isAttracted && attractTarget != null)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                attractTarget.position,
                attractSpeed * Time.deltaTime
            );
        }

        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRadius, playerLayer);

        if (hit != null)
        {
            Collect();
        }
    }

    public void AttractTo(Transform target, float speed)
    {
        isAttracted = true;
        attractTarget = target;
        attractSpeed = speed;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}