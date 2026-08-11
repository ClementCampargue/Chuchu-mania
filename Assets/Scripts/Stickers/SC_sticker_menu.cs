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
    public TextMeshProUGUI number_of_stickers;
    public Image sprite_image;
    public List<GameObject> stars;
    public bool editing;
    public InputActionReference quit;
    public InputActionReference edit;
    private Material unlocked;
    public Material not_unlocked;

    public Button button;
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        SC_scursorManager.instance.gameObject.SetActive(false);
        SC_scursorManager.instance.enable_cursor();
        Invoke("delay",0.25f);
        unlocked = sprite_image.material;
        SC_scursorManager.instance.gameObject.SetActive(true);
    }

    void delay()
    {
        SC_scursorManager.instance.gameObject.SetActive(true);

    }
    private void OnEnable()
    {
        edit.action.Enable();
        quit.action.Enable();
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
        number_of_stickers.text = SC_StickerSaveSystem.instance.stickerCount.ToString("D2");
        if (edit.action.WasPerformedThisFrame())
        {
            start_edit_mode();
        }
        if (quit.action.WasPerformedThisFrame() && !SC_scursorManager.instance.grabing)
        {
            SC_screenshot_transition.instance.Capture("HUB");
        }

        if(!editing )
        {
            if (SC_controller_manager.instance.using_controller)
            {
                SC_scursorManager.instance.disable_cursor();
            }
            else
            {
                SC_scursorManager.instance.enable_cursor();
            }
        }
        else
        {
            SC_scursorManager.instance.enable_cursor();

        }

    }


    public void start_edit_mode()
    {
        SC_scursorManager.instance.enable_cursor();
        editing = true;
        anim.SetTrigger("On");
    }

    public void quit_edit_mode()
    {
        
        SC_scursorManager.instance.disable_cursor();
        if (SC_controller_manager.instance.using_controller)
        {
            button.Select();
        }
        editing = false;
        anim.SetTrigger("Off");
    }

    public void update_infos(SO_Sticker sticker, Button but)
    {
        if (sticker.unlocked)
        {
            button = but;
            sticker_name.text = sticker.sticker_name;
            sticker_description.text = sticker.description;
            if (sticker.special_mat)
            {
                sprite_image.material = sticker.special_mat;
            }
            else
            {
                sprite_image.material = unlocked;
            }
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
