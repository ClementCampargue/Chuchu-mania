using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SC_Sprite_animator : MonoBehaviour
{
    private SpriteRenderer spr;
    public List<Sprite> sprites;
    public float time_between_frames;
    public int counter;
    public bool loop = true;
    public bool random_start;
    private Image image;

    void Start()
    {
        if (sprites.Count <= 1) 
        { 
            this.enabled = false;
        }

        if (gameObject.GetComponent<Image>()!= null)
        {
            image = gameObject.GetComponent<Image>();
        }
        else if(gameObject.GetComponent<SpriteRenderer>() != null)
        {
            spr = gameObject.GetComponent<SpriteRenderer>();
        }
        if (random_start)
        {
            counter = Random.Range(0, sprites.Count);
        }
    }

    float elapsed = 0f;
    void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= time_between_frames)
        {
            elapsed = elapsed % time_between_frames;
            OutputTime();
        }
    }

    void OutputTime()
    {
        if (counter < sprites.Count - 1)
        {
            counter = counter + 1;
        }
        else if (loop)
        {
            counter = 0;
        }
        if (gameObject.GetComponent<Image>() != null)
        {
            image.sprite = sprites[counter];
            image.SetNativeSize();
        }
        else if (gameObject.GetComponent<SpriteRenderer>() != null)
        {
            spr.sprite = sprites[counter];
        }
    }
}
