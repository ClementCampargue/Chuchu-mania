using UnityEngine;

public class SC_sprite_scaler : MonoBehaviour
{
    public float maxWorldWidth = 1f; // taille MAX autorisée
    public float minScale = 0.1f;
    public float maxScale = 1f;

    public float finalMultiplier = 1f; 

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        ApplyFit();
    }

    void Update()
    {
        if (sr.sprite != null)
            ApplyFit();
    }

    void ApplyFit()
    {
        Sprite sprite = sr.sprite;
        if (sprite == null) return;

        Vector2 pixelSize = sprite.rect.size;
        float ppu = sprite.pixelsPerUnit;

        Vector2 worldSize = pixelSize / ppu;

        float scale = 1f;

        if (worldSize.x > maxWorldWidth)
        {
            scale = maxWorldWidth / worldSize.x;
        }

        scale = Mathf.Clamp(scale, minScale, maxScale);

        scale *= finalMultiplier;

        transform.localScale = Vector3.one * scale;
    }
}