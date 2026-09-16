using System.Collections.Generic;
using UnityEngine;

public class SC_balloon : MonoBehaviour
{
    [Header("Movement")]
    public bool player_on;
    public float speed = 2f;
    public float speed_player_on = 0.5f;

    [Header("Start Delay")]
    public float minStartDelay = 0.2f;
    public float maxStartDelay = 1.0f;

    private float startDelay;
    private bool canMove = false;

    [Header("Height")]
    public float max_height = 10f;

    [Header("Skin")]
    public int skin;

    [Header("Sprites")]
    public List<Sprite> skin_sprites;
    public List<Sprite> skin_sprites_player_on;

    public SpriteRenderer sprite_renderer;

    public Animator anim;

    private void Start()
    {
        // Choisit un skin aléatoire
        ChooseRandomSkin();

        // Génère un délai aléatoire avant le début du mouvement
        startDelay = Random.Range(minStartDelay, maxStartDelay);

        // Lance le compteur
        Invoke(nameof(StartMoving), startDelay);
    }

    private void Update()
    {
        // Tant que le délai n'est pas terminé, le ballon reste immobile
        if (!canMove)
            return;

        // Le ballon monte en continu
        float current_speed = player_on ? speed_player_on : speed;

        transform.position += Vector3.up * current_speed * Time.deltaTime;

        // Éclate lorsqu'il atteint la hauteur maximale WORLD
        if (transform.position.y >= max_height)
        {
            Destroy(gameObject);
        }
    }

    private void StartMoving()
    {
        canMove = true;
    }

    private void ChooseRandomSkin()
    {
        // Choix aléatoire
        int randomIndex = Random.Range(0, skin_sprites.Count);

        skin = randomIndex;

        ApplyNormalSprite();
    }

    private void ApplyNormalSprite()
    {
        if (sprite_renderer == null)
            return;

        if (skin >= 0 && skin < skin_sprites.Count)
        {
            sprite_renderer.sprite = skin_sprites[skin];
        }
    }

    private void ApplyPlayerOnSprite()
    {
        if (sprite_renderer == null)
            return;

        if (skin >= 0 && skin < skin_sprites_player_on.Count)
        {
            sprite_renderer.sprite = skin_sprites_player_on[skin];
        }
    }

    private void ApplyPlayerOffSprite()
    {
        if (sprite_renderer == null)
            return;

        if (skin >= 0 && skin < skin_sprites.Count)
        {
            sprite_renderer.sprite = skin_sprites[skin];
        }
    }

    private void PopBalloon()
    {
        anim.enabled = true;
        this.enabled = false;
    }

    // Pour un jeu 2D avec un Collider2D sur le ballon
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (SC_player.instance.rb.linearVelocityY < 0.01f &&
                SC_player.instance.rb.linearVelocityY > -0.01f)
            {
                PlayerOn();
            }
        }
 
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Dard"))
        {
            PopBalloon();
        }

    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerOff();
        }
    }

    private void PlayerOn()
    {
        if (player_on)
            return;

        player_on = true;

        // Le skin reste identique, seul le sprite change
        ApplyPlayerOnSprite();
    }

    private void PlayerOff()
    {
        if (!player_on)
            return;

        player_on = false;

        // Le skin reste identique, seul le sprite change
        ApplyPlayerOffSprite();
    }

    private void OnDrawGizmos()
    {
        // max_height est une hauteur ABSOLUE dans le World Space
        float maxHeight = max_height;

        // Ligne représentant la hauteur maximale
        Gizmos.color = Color.red;

        Vector3 start = new Vector3(
            transform.position.x - 0.5f,
            maxHeight,
            transform.position.z
        );

        Vector3 end = new Vector3(
            transform.position.x + 0.5f,
            maxHeight,
            transform.position.z
        );

        Gizmos.DrawLine(start, end);

        // Petite ligne verticale entre le ballon et la hauteur maximale
        Gizmos.DrawLine(
            new Vector3(
                transform.position.x,
                transform.position.y,
                transform.position.z
            ),
            new Vector3(
                transform.position.x,
                maxHeight,
                transform.position.z
            )
        );
    }
}
