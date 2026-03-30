using UnityEngine;

public class SC_enemy_movement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public Transform groundDetection;
    public float detectionDistance = 1f;
    public float flipCooldown = 0.2f;

    [Header("Screen Wrap")]
    public float leftLimit = -10f;
    public float rightLimit = 10f;
    public GameObject ghostPrefab; // prefab contenant SpriteRenderer et Animator

    private bool movingRight = true;
    private Rigidbody2D rb;
    private float lastFlipTime = 0f;
    public SpriteRenderer spriteRenderer;
    public LayerMask groundLayer;

    private GameObject ghost; // clone pour le wrap
    private float levelWidth;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        levelWidth = rightLimit - leftLimit;

        // Instancie le ghost
        if (ghostPrefab != null)
        {
            ghost = Instantiate(ghostPrefab);
            ghost.SetActive(false);
        }
    }

    void FixedUpdate()
    {
        // Déplacement horizontal
        rb.linearVelocity = new Vector2(movingRight ? moveSpeed : -moveSpeed, rb.linearVelocity.y);

        // Vérifie sol et mur
        bool isGroundAhead = Physics2D.Raycast(groundDetection.position, Vector2.down, detectionDistance, groundLayer);
        bool isWallAhead = Physics2D.Raycast(groundDetection.position, movingRight ? Vector2.right : Vector2.left, 0.1f, groundLayer);

        if ((!isGroundAhead || isWallAhead) && Time.time - lastFlipTime > flipCooldown)
        {
            Flip();
            lastFlipTime = Time.time;
        }

        // Update ghost horizontal
        UpdateGhost();

        // Téléportation réelle
        Vector3 pos = transform.position;
        if (pos.x > rightLimit) pos.x = leftLimit;
        else if (pos.x < leftLimit) pos.x = rightLimit;
        transform.position = pos;
    }

    void Flip()
    {
        movingRight = !movingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    void UpdateGhost()
    {
        if (ghost == null) return;

        Vector3 pos = transform.position;

        // Si proche du bord, active le ghost de l'autre côté
        if (pos.x > rightLimit - levelWidth / 2)
        {
            ghost.SetActive(true);
            ghost.transform.position = new Vector3(pos.x - levelWidth, pos.y, pos.z);
            ghost.transform.localScale = transform.localScale;
        }
        else if (pos.x < leftLimit + levelWidth / 2)
        {
            ghost.SetActive(true);
            ghost.transform.position = new Vector3(pos.x + levelWidth, pos.y, pos.z);
            ghost.transform.localScale = transform.localScale;
        }
        else
        {
            ghost.SetActive(false);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundDetection != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(groundDetection.position, groundDetection.position + Vector3.down * detectionDistance);
            Gizmos.DrawLine(groundDetection.position, groundDetection.position + (movingRight ? Vector3.right : Vector3.left) * 0.1f);
        }
    }
}