using UnityEngine;

public class SC_scroll_sprite : MonoBehaviour
{
    public Vector2 scrollSpeed = new Vector2(1f, 0f);

    private SpriteRenderer sr;
    private Material mat;
    private Vector2 offset;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        mat = sr.material;
    }

    void Update()
    {
        offset += scrollSpeed * Time.deltaTime;

        mat.mainTextureOffset = offset;
    }
}