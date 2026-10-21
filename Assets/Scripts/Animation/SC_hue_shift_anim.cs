
using UnityEngine;

public class SC_hue_shift_anim : MonoBehaviour
{
    [Header("Hue Shift")]
    [Range(0f, 2f)]
    public float speed = 0.5f;

    [Range(0f, 1f)]
    public float saturation = 1f;

    [Range(0f, 1f)]
    public float brightness = 1f;

    private SpriteRenderer spriteRenderer;
    private float hue;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (spriteRenderer == null) return;

        hue = (hue + speed * Time.deltaTime) % 1f;

        Color color = Color.HSVToRGB(
            hue,
            saturation,
            brightness
        );

        // Préserve la transparence du sprite
        color.a = spriteRenderer.color.a;

        spriteRenderer.color = color;
    }
}
