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
    public float maxX = 8f;
    public float maxY = 4.5f;

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
        if (position.x <= -maxX)
        {
            position.x = -maxX;

            direction.x = Mathf.Abs(direction.x);

            hitWall = true;
        }

        // Bord droit
        else if (position.x >= maxX)
        {
            position.x = maxX;

            direction.x = -Mathf.Abs(direction.x);

            hitWall = true;
        }

        // Bord bas
        if (position.y <= -maxY)
        {
            position.y = -maxY;

            direction.y = Mathf.Abs(direction.y);

            hitWall = true;
        }

        // Bord haut
        else if (position.y >= maxY)
        {
            position.y = maxY;

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

        // Nouvel angle aléatoire entre 30° et 60°
        float angle = Random.Range(
            minBounceAngle,
            maxBounceAngle
        );

        if (Random.value < 0.5f)
        {
            angle *= -1f;
        }

        // Rebond horizontal
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            float x = Mathf.Sign(direction.x);
            float y = Mathf.Tan(angle * Mathf.Deg2Rad) * x;

            direction = new Vector2(x, y);
        }
        // Rebond vertical
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

            die();

            // Si la cage a encore des points de vie,
            // elle repart dans une nouvelle direction.
            if (Health > 0)
            {
                StartBouncing();
            }
        }
    }

    private void StartBouncing()
    {
        isBouncing = true;

        // Nouvelle vitesse
        currentSpeed = startSpeed;

        // Nouvelle direction aléatoire
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

    private void die()
    {
        // IMPORTANT :
        // le mouvement est toujours arrêté au moment du coup.
        isBouncing = false;
        currentSpeed = 0f;
        SC_player.instance.anim_.SetTrigger("Punch");

        Health--;

        if (Health <= 0)
        {
            Health = 0;

            anim.SetTrigger("Die");

            fire_system.SetActive(false);

            juice_death.PlayJuice();

            Time.timeScale = 0.25f;

            spr.material = player_green;

            SC_player.instance.enabled = false;

            SC_player.instance.collider.enabled = false;

            SC_player.instance.rb.gravityScale = 0;

            SC_player.instance.rb.constraints =
                RigidbodyConstraints2D.FreezeAll;

            SC_player.instance.anim_.SetTrigger("End");

            SC_music_manager.instance.stop_music();
        }
        else
        {
            anim.SetTrigger("Damage");

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
}
