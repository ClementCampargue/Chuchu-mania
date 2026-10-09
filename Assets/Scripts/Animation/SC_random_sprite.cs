 using UnityEngine;

public class SC_random_sprite : MonoBehaviour
{
    public Sprite[] sprites;

    void Start()
    {
        if (sprites.Length > 0)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                sr.sprite = sprites[Random.Range(0, sprites.Length)];
            }
        }
    }
}
