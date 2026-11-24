
using UnityEngine;
using System.Collections;

public class SC_particle_trail : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer playerSprite;
    public Animator animator;

    [Header("Trail Settings")]
    public float spawnRate = 0.05f;
    public float lifetime = 0.5f;

    [Header("Appearance")]
    public Vector3 offset = Vector3.zero;
    public float scale = 1f;

    [Header("Material")]
    public Material trailMaterial;

    [Header("Color Ramp")]
    public Gradient colorRamp = new Gradient();

    [Header("Animation")]
    public bool trailOnlyWhenMoving = true;
    public float minSpeed = 0.1f;

    private float timer;
    private Vector3 lastPosition;

    void Start()
    {
        if (playerSprite == null)
            playerSprite = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        lastPosition = transform.position;
    }

    void Update()
    {
        if (playerSprite == null || playerSprite.sprite == null)
            return;

        float speed = Vector3.Distance(
            transform.position,
            lastPosition
        ) / Mathf.Max(Time.deltaTime, 0.0001f);

        lastPosition = transform.position;

        bool shouldSpawn =
            !trailOnlyWhenMoving || speed > minSpeed;

        if (shouldSpawn)
        {
            timer += Time.deltaTime;

            if (timer >= spawnRate)
            {
                SpawnTrail();
                timer = 0f;
            }
        }
        else
        {
            timer = 0f;
        }
    }

    void SpawnTrail()
    {
        GameObject ghost = new GameObject("PlayerTrail");

        ghost.transform.position = playerSprite.transform.position + offset;
        ghost.transform.rotation = playerSprite.transform.rotation;
        ghost.transform.localScale =
            playerSprite.transform.lossyScale * scale;

        SpriteRenderer ghostRenderer =
            ghost.AddComponent<SpriteRenderer>();

        ghostRenderer.sprite = playerSprite.sprite;
        ghostRenderer.flipX = playerSprite.flipX;
        ghostRenderer.flipY = playerSprite.flipY;

        ghostRenderer.sortingLayerID =
            playerSprite.sortingLayerID;

        ghostRenderer.sortingOrder =
            playerSprite.sortingOrder - 1;

        // Material
        ghostRenderer.sharedMaterial =
            trailMaterial != null
            ? trailMaterial
            : playerSprite.sharedMaterial;

        StartCoroutine(FadeTrail(ghostRenderer));
    }

    IEnumerator FadeTrail(SpriteRenderer ghost)
    {
        float elapsed = 0f;

        while (elapsed < lifetime)
        {
            if (ghost == null)
                yield break;

            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / lifetime);

            // Color Ramp
            ghost.color = colorRamp.Evaluate(t);

            yield return null;
        }

        if (ghost != null)
            Destroy(ghost.gameObject);
    }
}
