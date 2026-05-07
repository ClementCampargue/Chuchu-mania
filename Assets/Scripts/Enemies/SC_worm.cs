using UnityEngine;

public class SC_enemy_random_teleport : MonoBehaviour
{
    [Header("Teleport Points")]
    public Transform[] teleportPoints;

    [Header("Teleport")]
    public float teleportInterval = 3f;

    [Header("Detection")]
    public Vector2 checkSize = new Vector2(1f, 1f);

    [Tooltip("Offset appliqué uniquement à la zone de détection")]
    public Vector2 detectionOffset = new Vector2(0f, 0f);

    [Tooltip("Objets empêchant le TP")]
    public LayerMask blockedLayers;

    private float timer;

    public Animator anim;
    private void Start()
    {
        timer = Random.Range(teleportInterval/2, teleportInterval);
    }
    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= teleportInterval)
        {
            timer = 0f;
            teleport();
        }
    }

     void teleport()
    {
        anim.SetTrigger("teleport");

    }

    public  void TeleportToRandomPoint()
    {
        if (teleportPoints.Length == 0)
            return;

        int randomStart = Random.Range(0, teleportPoints.Length);

        for (int i = 0; i < teleportPoints.Length; i++)
        {
            int index = (randomStart + i) % teleportPoints.Length;

            Transform targetPoint = teleportPoints[index];

            Vector2 checkPos =
                (Vector2)targetPoint.position + detectionOffset;

            // Check rectangle avec offset
            Collider2D blocked =
                Physics2D.OverlapBox(
                    checkPos,
                    checkSize,
                    0f,
                    blockedLayers
                );

            if (blocked == null)
            {
                transform.position = targetPoint.position;
                return;
            }
        }

        Debug.Log("Aucun point valide pour le téléport.");
    }

    void OnDrawGizmosSelected()
    {
        if (teleportPoints == null)
            return;

        Gizmos.color = Color.cyan;

        foreach (Transform point in teleportPoints)
        {
            if (point != null)
            {
                Vector2 checkPos =
                    (Vector2)point.position + detectionOffset;

                Gizmos.DrawWireCube(checkPos, checkSize);
            }
        }
    }
}