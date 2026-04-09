using UnityEngine;

public class SC_magnet_power_up : MonoBehaviour
{
    public static SC_magnet_power_up Instance;

    [Header("Magnet Settings")]
    public float radius = 5f;
    public float attractionSpeed = 10f;
    public LayerMask collectibleLayer;

    private bool isActive = false;
    public ParticleSystem ps;
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
    }

    void Update()
    {
        if (!isActive) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, collectibleLayer);

        foreach (var hit in hits)
        {
            SC_starbit collectible = hit.GetComponent<SC_starbit>();

            if (collectible != null)
            {
                collectible.AttractTo(transform, attractionSpeed);
            }
        }
    }

    public void ActivateMagnet(float duration)
    {
        ps.Play();
        StopAllCoroutines();
        StartCoroutine(MagnetCoroutine(duration));
    }

    System.Collections.IEnumerator MagnetCoroutine(float duration)
    {
        isActive = true;

        yield return new WaitForSeconds(duration);
        ps.Stop();

        isActive = false;
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
