using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;

public class SC_sticker_popup : MonoBehaviour
{
    public static SC_sticker_popup instance;

    [Header("UI")]
    public TextMeshPro name_;
    public TextMeshPro rarity;
    public SpriteRenderer spr;
    public TextMeshPro title;
    public LocalizedString sticker_obtenu;
    public LocalizedString item_obtenu;
    [Header("Localization")]
    public LocalizedString common;
    public LocalizedString rare;
    public LocalizedString epic;

    [Header("Input")]
    public InputActionReference submit;
    public InputActionReference submit2;

    [Header("Animation")]
    public Animator anim;

    [Header("Settings")]
    public bool can_quit;
    public GameObject new_;
    public GameObject rarity_;
    // --------------------------------------------------
    // QUEUES
    // --------------------------------------------------

    private SO_Sticker[] stickerQueue;
    private SO_shop_item1[] itemQueue;

    private int currentIndex = 0;

    // true = stickers
    // false = items
    private bool showingStickers = true;

    private bool isShowingSequence = false;
    private bool isClosing = false;

    // --------------------------------------------------
    // UNITY
    // --------------------------------------------------

    private void Awake()
    {
        instance = this;

        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        can_quit = false;
        isClosing = false;

        CancelInvoke();

        Invoke(nameof(AllowQuit), 0.5f);

        if (anim != null)
        {
            if (anim.enabled)
            {
                anim.ResetTrigger("hide");
                anim.ResetTrigger("get");
                anim.SetTrigger("show");
            }
            else
            {
                anim.enabled = true;
                anim.ResetTrigger("hide");
                anim.ResetTrigger("get");
                anim.SetTrigger("show");
            }
        }

        // Si le popup est ouvert depuis le Shop,
        // on bloque la fermeture du menu.
        if (SceneManager.GetActiveScene().name == "Shop")
        {
            if (SC_shop_manager.instance != null)
            {
                SC_shop_manager.instance.canquit = false;
            }
        }
    }

    private void Update()
    {
        if (!isShowingSequence || isClosing)
            return;

        bool pressedSubmit =
            submit != null &&
            submit.action != null &&
            submit.action.WasPressedThisFrame();

        bool pressedSubmit2 =
            submit2 != null &&
            submit2.action != null &&
            submit2.action.WasPressedThisFrame();

        if ((pressedSubmit || pressedSubmit2) && can_quit)
        {
            ShowNext();
        }
    }

    // --------------------------------------------------
    // INPUT DELAY
    // --------------------------------------------------

    private void AllowQuit()
    {
        if (!isClosing)
        {
            can_quit = true;
        }
    }

    // --------------------------------------------------
    // SINGLE STICKER
    // --------------------------------------------------

    public void update_visuals(SO_Sticker sticker)
    {
        if (sticker == null)
            return;
        title.text = sticker_obtenu.GetLocalizedString();

        stickerQueue = new SO_Sticker[1];
        stickerQueue[0] = sticker;
        rarity_.SetActive(true);

        itemQueue = null;
        new_.SetActive(!sticker.unlocked);

        currentIndex = 0;

        showingStickers = true;
        isShowingSequence = true;
        isClosing = false;
        can_quit = false;

        gameObject.SetActive(true);

        DisplayCurrent();
    }

    // --------------------------------------------------
    // SINGLE ITEM
    // --------------------------------------------------

    public void update_visuals(SO_shop_item1 item)
    {
        if (item == null)
            return;
        title.text = item_obtenu.GetLocalizedString();
        itemQueue = new SO_shop_item1[1];
        itemQueue[0] = item;
        new_.SetActive(false);
        rarity_.SetActive(false);
        stickerQueue = null;

        currentIndex = 0;

        showingStickers = false;
        isShowingSequence = true;
        isClosing = false;
        can_quit = false;

        gameObject.SetActive(true);

        DisplayCurrent();
    }

    // --------------------------------------------------
    // MULTIPLE STICKERS
    // --------------------------------------------------

    public void ShowStickers(SO_Sticker[] stickersToShow)
    {
        if (stickersToShow == null || stickersToShow.Length == 0)
            return;

        stickerQueue = stickersToShow;
        itemQueue = null;

        currentIndex = 0;

        showingStickers = true;
        isShowingSequence = true;
        isClosing = false;
        can_quit = false;

        gameObject.SetActive(true);

        DisplayCurrent();
    }

    // --------------------------------------------------
    // MULTIPLE ITEMS
    // --------------------------------------------------

    public void ShowItems(SO_shop_item1[] itemsToShow)
    {
        if (itemsToShow == null || itemsToShow.Length == 0)
            return;

        itemQueue = itemsToShow;
        stickerQueue = null;

        currentIndex = 0;

        showingStickers = false;
        isShowingSequence = true;
        isClosing = false;
        can_quit = false;

        gameObject.SetActive(true);

        DisplayCurrent();
    }

    // --------------------------------------------------
    // DISPLAY CURRENT
    // --------------------------------------------------

    private void DisplayCurrent()
    {
        if (showingStickers)
        {
            if (stickerQueue == null)
                return;

            if (currentIndex < 0 || currentIndex >= stickerQueue.Length)
                return;

            DisplaySticker(stickerQueue[currentIndex]);
        }
        else
        {
            if (itemQueue == null)
                return;

            if (currentIndex < 0 || currentIndex >= itemQueue.Length)
                return;

            DisplayItem(itemQueue[currentIndex]);
        }
    }

    // --------------------------------------------------
    // DISPLAY STICKER
    // --------------------------------------------------

    private void DisplaySticker(SO_Sticker sticker)
    {
        if (sticker == null)
            return;

        if (spr != null)
        {
            spr.sprite = sticker.sticker_sprite;
        }

        if (name_ != null)
        {
            name_.text = sticker.sticker_name;
        }

        if (rarity != null)
        {
            switch (sticker.rarity)
            {
                case 0:
                    rarity.text = common.GetLocalizedString();
                    break;

                case 1:
                    rarity.text = rare.GetLocalizedString();
                    break;

                case 2:
                    rarity.text = epic.GetLocalizedString();
                    break;

                default:
                    rarity.text = "";
                    break;
            }
        }
    }

    // --------------------------------------------------
    // DISPLAY ITEM
    // --------------------------------------------------

    private void DisplayItem(SO_shop_item1 item)
    {
        if (item == null)
            return;

        if (spr != null)
        {
            spr.sprite = item.item_sprite;
        }

        if (name_ != null)
        {
            name_.text = item.item_name;
        }

        // Un item n'a pas de rareté.
        if (rarity != null)
        {
            rarity.text = "";
        }
    }

    // --------------------------------------------------
    // NEXT
    // --------------------------------------------------

    private void ShowNext()
    {
        if (isClosing)
            return;

        can_quit = false;

        currentIndex++;

        bool hasNext = false;

        if (showingStickers)
        {
            hasNext =
                stickerQueue != null &&
                currentIndex < stickerQueue.Length;
        }
        else
        {
            hasNext =
                itemQueue != null &&
                currentIndex < itemQueue.Length;
        }

        // ----------------------------------------------
        // IL RESTE UNE RÉCOMPENSE
        // ----------------------------------------------

        if (hasNext)
        {
            DisplayCurrent();

            if (anim != null)
            {
                anim.ResetTrigger("get");
                anim.SetTrigger("get");
            }

            Invoke(nameof(AllowNext), 0.5f);
        }
        // ----------------------------------------------
        // PLUS RIEN
        // ----------------------------------------------
        else
        {
            Close();
        }
    }

    // --------------------------------------------------
    // ALLOW NEXT
    // --------------------------------------------------

    private void AllowNext()
    {
        if (!isClosing)
        {
            can_quit = true;
        }
    }

    // --------------------------------------------------
    // CLOSE
    // --------------------------------------------------

    public void Close()
    {
        if (!isShowingSequence || isClosing)
            return;

        isClosing = true;
        isShowingSequence = false;
        can_quit = false;

        CancelInvoke();

        // ----------------------------------------------
        // SHOP
        // ----------------------------------------------

        if (SceneManager.GetActiveScene().name == "Shop")
        {
            if (SC_shop_manager.instance != null)
            {
                SC_shop_manager.instance.show_menu();
            }
        }

        // ----------------------------------------------
        // ACHIEVEMENTS
        // ----------------------------------------------

        if (SceneManager.GetActiveScene().name == "Achievements")
        {
            if (SC_ConstellationNavigation.instance != null)
            {
                SC_ConstellationNavigation.instance.enabled = true;
            }

            if (SC_achievement_menu.instance != null)
            {
                SC_achievement_menu.instance.show_sticker();
            }
        }

        // ----------------------------------------------
        // ANIMATION
        // ----------------------------------------------

        if (anim != null)
        {
            anim.ResetTrigger("get");
            anim.ResetTrigger("show");
            anim.SetTrigger("hide");
        }

        StartCoroutine(DisableAfterHide());
    }

    // --------------------------------------------------
    // DISABLE AFTER HIDE
    // --------------------------------------------------

    private IEnumerator DisableAfterHide()
    {
        yield return new WaitForSeconds(0.5f);

        gameObject.SetActive(false);

        isClosing = false;

        // Nettoyage
        stickerQueue = null;
        itemQueue = null;
        currentIndex = 0;
    }
}
