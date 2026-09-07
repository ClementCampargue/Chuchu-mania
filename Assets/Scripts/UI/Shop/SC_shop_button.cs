using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.Localization.SmartFormat.Utilities;
using UnityEngine.Localization;

public class SC_shop_button : MonoBehaviour
{
    public SpriteRenderer spr;

    public SO_Sticker sticker;

    public InputActionReference selectAction;

    private bool selected;

    public TextMeshPro price;
    public LocalizedString description;
    private LocalizedString description_;
    public SC_shop_manager shop;
    public bool random_sticker;
    public SC_Button button;
    public Animator anim;
    public GameObject cant_buy_visual;
    private void OnEnable()
    {
        if (!random_sticker)
        {
            spr.sprite = sticker.sticker_sprite;
            price.text = sticker.Price.ToString() + "$";
            if (SC_money_manager.instance.money < sticker.Price)
            {
                cant_buy_visual.SetActive(true);
            }

        }
        else
        {
            if (SC_money_manager.instance.money < 25)
            {
                cant_buy_visual.SetActive(true);
            }
     
        }

            selectAction.action.Enable();


    }

    private void Start()
    {
        description_ = description;

    }

    private void Update()
    {
        if (button.isHovered || button.isSelected)
        {
            if (shop.typewriter.fullText != description.GetLocalizedString())
            {
                shop.show_text(description.GetLocalizedString());
            }

            selected = true;
            if (selectAction.action.WasPressedThisFrame())
            {
                submit();
            }

        }
        else
        {
            description = description_;

        }

    }
    public void submit()
    {
        if (random_sticker)
        {
            if (SC_money_manager.instance.money >= 25)
            {
                shop.Show_gacha();

            }
            else
            {
                anim.ResetTrigger("Hover");
                anim.ResetTrigger("Unhover");
                anim.ResetTrigger("Press");
                anim.SetTrigger("cant_buy");
                description = shop.cant_buy_;
            }
        }
        else
        {
            if(SC_money_manager.instance.money>= sticker.Price)
            {
                SC_money_shop.instance.Buy(sticker.Price);
                shop.buttons_.SetActive(false);
                SC_sticker_popup.instance.update_visuals(sticker);
                sticker.unlocked = true;
                description = shop.thanks;
            }
            else
            {
                anim.ResetTrigger("Hover");
                anim.ResetTrigger("Unhover");
                anim.ResetTrigger("Press");
                anim.SetTrigger("cant_buy");
                description = shop.cant_buy_;
            }

        }
    }
}