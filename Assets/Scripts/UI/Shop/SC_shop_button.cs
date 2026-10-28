
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.Localization;

public class SC_shop_button : MonoBehaviour
{
    public SpriteRenderer spr;

    public SO_Sticker sticker;
    public SO_sticker_list stickers;
    public SO_shop_item1 item;

    public InputActionReference selectAction;

    private bool selected;

    public int price_;
    public TextMeshPro price;
    public LocalizedString description;

    private LocalizedString description_;

    public SC_shop_manager shop;
    public bool random_sticker;
    public SC_Button button;
    public Animator anim;
    public GameObject cant_buy_visual;
    public GameObject new_;

    public AudioSource meow;
    public AudioSource buy;
    public AudioSource cantbuy;

    public bool wish;

    private SC_SaveManager SaveManager => SC_SaveManager.instance;

    private void Awake()
    {
        description_ = description;

        if (stickers != null &&
            stickers.stickers != null &&
            stickers.stickers.Count > 0)
        {
            sticker = stickers.stickers[
                Random.Range(0, stickers.stickers.Count)
            ];
        }
    }

    private void OnEnable()
    {
        RefreshVisuals();

        if (selectAction != null && selectAction.action != null)
        {
            selectAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (selectAction != null && selectAction.action != null)
        {
            selectAction.action.Disable();
        }
    }

    private void RefreshVisuals()
    {
        if (SaveManager == null || SaveManager.data == null)
            return;

        if (!random_sticker && item == null)
        {
            if (sticker == null)
                return;

            if (spr != null)
                spr.sprite = sticker.sticker_sprite;

            if (price != null)
                price.text = sticker.Price + "$";

            if (cant_buy_visual != null)
            {
                cant_buy_visual.SetActive(
                    SaveManager.data.money < sticker.Price
                );
            }

            if (new_ != null)
            {
                new_.SetActive(
                    !SaveManager.IsStickerUnlocked(sticker)
                );
            }
        }
        else if (item != null)
        {
            if (spr != null)
                spr.sprite = item.item_sprite;

            if (price != null)
                price.text = item.Price + "$";

            if (cant_buy_visual != null)
            {
                cant_buy_visual.SetActive(
                    SaveManager.data.money < item.Price
                );
            }

            if (new_ != null)
                new_.SetActive(false);
        }
        else
        {
            if (price != null)
                price.text = price_ + "$";

            if (cant_buy_visual != null)
            {
                cant_buy_visual.SetActive(
                    SaveManager.data.money < 25
                );
            }

            if (new_ != null)
                new_.SetActive(false);
        }
    }

    private void Update()
    {
        if (button == null || shop == null)
            return;

        if (button.isHovered || button.isSelected)
        {
            if (shop.typewriter != null &&
                shop.typewriter.fullText != description.GetLocalizedString())
            {
                shop.show_text(description.GetLocalizedString());
            }

            selected = true;

            if (selectAction != null &&
                selectAction.action != null &&
                selectAction.action.WasPressedThisFrame())
            {
                submit();
            }
        }
        else
        {
            description = description_;
            selected = false;
        }
    }

    public void submit()
    {
        if (SaveManager == null || SaveManager.data == null)
            return;

        if (random_sticker)
        {
            if (SaveManager.data.money >= 25)
            {
                if (meow != null)
                    meow.Play();

                if (shop != null)
                    shop.Show_gacha();
            }
            else
            {
                CantBuy();
            }

            return;
        }

        if (sticker != null)
        {
            int stickerPrice = sticker.Price;

            if (SaveManager.data.money >= stickerPrice)
            {
                if (SaveManager.IsStickerUnlocked(sticker))
                {
                    // Déjà débloqué : ne pas le racheter.
                    return;
                }

                if (buy != null)
                    buy.Play();

                if (SC_money_shop.instance != null)
                    SC_money_shop.instance.future_price = stickerPrice;

                if (shop != null && shop.buttons_ != null)
                    shop.buttons_.SetActive(false);

                if (SC_sticker_popup.instance != null)
                {
                    SC_sticker_popup.instance.update_visuals(sticker);
                }
                else
                {
                    Debug.LogWarning(
                        "SC_shop_button : SC_sticker_popup.instance est null."
                    );
                }
            }
            else
            {
                CantBuy();
            }
        }
        else if (item != null)
        {
            if (SaveManager.data.money >= item.Price)
            {
                if (buy != null)
                    buy.Play();

                if (SC_money_shop.instance != null)
                    SC_money_shop.instance.future_price = item.Price;

                if (shop != null && shop.buttons_ != null)
                    shop.buttons_.SetActive(false);

                if (SC_sticker_popup.instance != null)
                {
                    SC_sticker_popup.instance.update_visuals(item);
                }

                if (wish)
                {
                    SaveManager.AddWish(1);
                }
            }
            else
            {
                CantBuy();
            }
        }
        else
        {
            if (SC_shop_manager.instance != null)
                SC_shop_manager.instance.Talk_Thx();
        }
    }

    private void CantBuy()
    {
        if (cantbuy != null)
            cantbuy.Play();

        if (anim != null)
        {
            anim.ResetTrigger("Hover");
            anim.ResetTrigger("Unhover");
            anim.ResetTrigger("Press");
            anim.SetTrigger("cant_buy");
        }

        if (shop != null)
            description = shop.cant_buy_;
    }
}
