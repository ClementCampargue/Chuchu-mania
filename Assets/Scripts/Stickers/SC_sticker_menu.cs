using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SC_sticker_menu : MonoBehaviour
{
    public static SC_sticker_menu instance;

    public Animator anim;

    public TextMeshProUGUI sticker_name;
    public TextMeshProUGUI sticker_description;
    public TextMeshProUGUI artist;
    public Image sprite_image;
    public List<GameObject> stars;


    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void start_edit_mode()
    {
        anim.SetTrigger("On");
    }

    public void quit_edit_mode()
    {
        anim.SetTrigger("Off");
    }

    public void update_infos(SO_Sticker sticker)
    {
        sticker_name.text = sticker.sticker_name;
        sticker_description.text = sticker.description;
        artist.text = sticker.artist;
        sprite_image.sprite = sticker.sticker_sprite;
        UpdateStars(sticker.rarity);
    }
    public void UpdateStars(int rarity)
    {
        foreach (GameObject star in stars)
        {
            star.SetActive(false);
        }

        rarity = Mathf.Clamp(rarity, 0, stars.Count);

        for (int i = 0; i < rarity; i++)
        {
            stars[i].SetActive(true);
        }
    }
}
