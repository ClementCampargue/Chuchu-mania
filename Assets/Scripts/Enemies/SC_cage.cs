using UnityEngine;

public class SC_cage : MonoBehaviour
{
    private SC_icecream_eat_system system;

    public int Health = 1;
    public GameObject fire_system;

    private SC_win_screen win_screen;

    public SC_juiciness juice_damage;
    public SC_juiciness juice_death;

    public Material player_green;
    private Material default_mat;

    private SpriteRenderer spr;

    public Animator anim;
    public AudioClip clip;

    [Header("Mouvement")]
    public float startSpeed = 8f;

    [Range(0.1f, 1f)]
    public float speedMultiplierOnBounce = 0.85f;

    public float minSpeed = 0.5f;

    [Header("Limites de l'écran")]
    public Vector2 screenMin = new Vector2(-8f, -4.5f);
    public Vector2 screenMax = new Vector2(8f, 4.5f);

    [Header("Angle des rebonds")]
    public float minBounceAngle = 30f;
    public float maxBounceAngle = 60f;

    private Vector2 direction;
    private float currentSpeed;

    private bool isBouncing = false;

    void Start()
    {
        system = SC_icecream_eat_system.instance;

        spr = SC_player.instance.spriteRendererPower;
        default_mat = spr.material;

        win_screen = SC_win_screen.instance;

        currentSpeed = 0f;
    }

    void Update()
    {
        if (!isBouncing)
            return;

        // Déplacement
        transform.position +=
            (Vector3)(direction.normalized * currentSpeed * Time.deltaTime);

        CheckScreenBounds();

        // Arrêt lorsque la vitesse est trop faible
        if (currentSpeed <= minSpeed)
        {
            currentSpeed = 0f;
            isBouncing = false;
        }
    }

    private void CheckScreenBounds()
    {
        Vector3 position = transform.position;

        bool hitWall = false;

        // Bord gauche
        if (position.x <= screenMin.x)
        {
            position.x = screenMin.x;

            direction.x = Mathf.Abs(direction.x);

            hitWall = true;
        }

        // Bord droit
        else if (position.x >= screenMax.x)
        {
            position.x = screenMax.x;

            direction.x = -Mathf.Abs(direction.x);

            hitWall = true;
        }

        // Bord bas
        if (position.y <= screenMin.y)
        {
            position.y = screenMin.y;

            direction.y = Mathf.Abs(direction.y);

            hitWall = true;
        }

        // Bord haut
        else if (position.y >= screenMax.y)
        {
            position.y = screenMax.y;

            direction.y = -Mathf.Abs(direction.y);

            hitWall = true;
        }

        transform.position = position;

        if (hitWall)
        {
            Bounce();
        }
    }

    private void Bounce()
    {
        // Réduit la vitesse à chaque rebond
        currentSpeed *= speedMultiplierOnBounce;

        float angle = Random.Range(
            minBounceAngle,
            maxBounceAngle
        );

        if (Random.value < 0.5f)
            angle *= -1f;

        // Si le mouvement est principalement horizontal
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            float x = Mathf.Sign(direction.x);
            float y = Mathf.Tan(angle * Mathf.Deg2Rad) * x;

            direction = new Vector2(x, y);
        }
        // Si le mouvement est principalement vertical
        else
        {
            float y = Mathf.Sign(direction.y);
            float x = Mathf.Tan(angle * Mathf.Deg2Rad) * y;

            direction = new Vector2(x, y);
        }

        direction.Normalize();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") &&
            system.isPowerUpActive &&
            Health > 0)
        {
            Debug.Log("Cage frappée !");

            // Direction de l'éjection basée sur la position du joueur
            SetDirectionFromPlayer();

            die();

            // Si la cage a encore des points de vie,
            // elle repart dans la direction du coup.
            if (Health > 0)
            {
                StartBouncing();
            }
        }
    }

    private void SetDirectionFromPlayer()
    {
        Vector2 playerPosition = SC_player.instance.transform.position;
        Vector2 cagePosition = transform.position;

        Vector2 hitDirection = cagePosition - playerPosition;

        // On évite une direction nulle
        if (hitDirection.sqrMagnitude < 0.01f)
        {
            hitDirection = Vector2.right;
        }

        hitDirection.Normalize();

        direction = hitDirection;
    }

    private void StartBouncing()
    {
        isBouncing = true;

        // Nouvelle vitesse
        currentSpeed = startSpeed;

        // Si aucune direction n'a encore été définie,
        // on crée une direction aléatoire.
        if (direction.sqrMagnitude < 0.01f)
        {
            float angle = Random.Range(30f, 60f);

            float xDirection =
                Random.value < 0.5f ? -1f : 1f;

            float yDirection =
                Random.value < 0.5f ? -1f : 1f;

            direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad) * xDirection,
                Mathf.Sin(angle * Mathf.Deg2Rad) * yDirection
            );

            direction.Normalize();
        }
    }

    private void die()
    {
        // IMPORTANT :
        // le mouvement est toujours arrêté au moment du coup.
        isBouncing = false;
        currentSpeed = 0f;

        Health--;

        if (Health <= 0)
        {
            Health = 0;
            SC_timer.instance.pause();
            anim.SetTrigger("Die");

            fire_system.SetActive(false);

            juice_death.PlayJuice();

            Time.timeScale = 0.25f;

            spr.material = player_green;


            SC_player.instance.collider.enabled = false;

            SC_player.instance.rb.gravityScale = 0;

            SC_player.instance.rb.constraints =
                RigidbodyConstraints2D.FreezeAll;

            SC_player.instance.anim .SetTrigger("End");
            SC_player.instance.enabled = false;

            SC_music_manager.instance.stop_music();
        }
        else
        {
            anim.SetTrigger("Damage");

            SC_player.instance.anim.SetTrigger("Punch");

            juice_damage.PlayJuice();
        }
    }

    public void RestorePlayerMat()
    {
        spr.material = default_mat;

        SC_player.instance.gameObject.SetActive(false);
    }

    public void win_sc()
    {
        win_screen.Start_screen();

        Time.timeScale = 0f;
    }

    public void play_music()
    {
        SC_music_manager.instance.update_music(clip, false);
    }
private void OnDrawGizmosSelected()
    {
        // Centre de la zone
        Vector2 center = (screenMin + screenMax) * 0.5f;

        // Taille de la zone
        Vector2 size = screenMax - screenMin;

        // Dessine la zone
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);

        // Dessine les 4 coins
        Gizmos.DrawSphere(screenMin, 0.1f);
        Gizmos.DrawSphere(
            new Vector2(screenMax.x, screenMin.y),
            0.1f
        );
        Gizmos.DrawSphere(
            new Vector2(screenMin.x, screenMax.y),
            0.1f
        );
        Gizmos.DrawSphere(screenMax, 0.1f);
    }

}
