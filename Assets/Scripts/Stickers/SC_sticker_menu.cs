using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
    public bool editing;
    public InputActionReference quit;
    private Material unlocked;
    public Material not_unlocked;
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        unlocked = sprite_image.material;
        SC_scursorManager.instance.gameObject.SetActive(true);
    }
    private void OnDisable()
    {
        SC_scursorManager.instance.gameObject.SetActive(false);

    }
    private void OnDestroy()
    {
        SC_scursorManager.instance.gameObject.SetActive(false);

    }
    // Update is called once per frame
    void Update()
    {
        if (quit.action.WasPerformedThisFrame())
        {
            SC_screenshot_transition.instance.Capture("HUB");
        }
    }

    public void start_edit_mode()
    {
        editing = true;
        anim.SetTrigger("On");
    }

    public void quit_edit_mode()
    {
        editing = false;
        anim.SetTrigger("Off");
    }

    public void update_infos(SO_Sticker sticker)
    {
        if (sticker.unlocked)
        {
            sticker_name.text = sticker.sticker_name;
            sticker_description.text = sticker.description;
            sprite_image.material = unlocked;
        }
        else
        {
            sticker_name.text = "???";
            sticker_description.text = sticker.unlock_conditions;
            sprite_image.material = not_unlocked;

        }
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
