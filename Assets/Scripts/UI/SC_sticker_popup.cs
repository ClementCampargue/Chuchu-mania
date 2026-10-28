
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

    [Header("Audio")]
    public AudioSource source;
    public AudioClip common_sfx;
    public AudioClip rare_sfx;

    private SO_Sticker[] stickerQueue;
    private SO_shop_item1[] itemQueue;

    private int currentIndex = 0;

    // true = stickers ; false = items
    private bool showingStickers = true;

    private bool isShowingSequence = false;
    private bool isClosing = false;

    private SC_SaveManager saveManager;

    // --------------------------------------------------
    // UNITY
    // --------------------------------------------------

    private void Awake()
    {
        instance = this;
        saveManager = SC_SaveManager.instance;
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
            anim.enabled = true;
            anim.ResetTrigger("hide");
            anim.ResetTrigger("get");
            anim.SetTrigger("show");
        }

        if (SceneManager.GetActiveScene().name == "Shop")
        {
            if (SC_shop_manager.instance != null)
            {
                if (SC_shop_manager.instance.anim != null)
                {
                    SC_shop_manager.instance.anim.ResetTrigger("Show_menu");
                    SC_shop_manager.instance.anim.SetTrigger("talk");
                }

                if (SC_shop_manager.instance.typewriter != null)
                    SC_shop_manager.instance.typewriter.clear_text();

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
            ShowNext();
    }

    private void AllowQuit()
    {
        if (!isClosing)
            can_quit = true;
    }

    private SC_SaveManager GetSaveManager()
    {
        if (saveManager == null)
            saveManager = SC_SaveManager.instance;

        return saveManager;
    }

    // --------------------------------------------------
    // SINGLE STICKER
    // --------------------------------------------------

    public void update_visuals(SO_Sticker sticker)
    {
        if (sticker == null)
            return;

        if (title != null)
            title.text = sticker_obtenu.GetLocalizedString();

        stickerQueue = new SO_Sticker[] { sticker };
        itemQueue = null;

        if (rarity_ != null)
            rarity_.SetActive(true);

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

        if (title != null)
            title.text = item_obtenu.GetLocalizedString();

        itemQueue = new SO_shop_item1[] { item };
        stickerQueue = null;

        if (new_ != null)
            new_.SetActive(false);

        if (rarity_ != null)
            rarity_.SetActive(false);

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

        if (title != null)
            title.text = sticker_obtenu.GetLocalizedString();

        if (rarity_ != null)
            rarity_.SetActive(true);

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

        if (title != null)
            title.text = item_obtenu.GetLocalizedString();

        if (new_ != null)
            new_.SetActive(false);

        if (rarity_ != null)
            rarity_.SetActive(false);

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
            if (stickerQueue == null ||
                currentIndex < 0 ||
                currentIndex >= stickerQueue.Length)
                return;

            DisplaySticker(stickerQueue[currentIndex]);
        }
        else
        {
            if (itemQueue == null ||
                currentIndex < 0 ||
                currentIndex >= itemQueue.Length)
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

        SC_SaveManager manager = GetSaveManager();

        if (manager == null)
        {
            Debug.LogError(
                "SC_SaveManager introuvable : impossible d'attribuer le sticker."
            );
            return;
        }

        if (manager.data == null)
        {
            Debug.LogError(
                "Aucune sauvegarde active : impossible d'attribuer le sticker."
            );
            return;
        }

        if (SC_music_manager.instance != null)
            SC_music_manager.instance.pause_music();

        // Vérifie l'état AVANT le déblocage.
        bool wasUnlocked = manager.IsStickerUnlocked(sticker);

        // Indique si le sticker vient d'être obtenu.
        if (new_ != null)
            new_.SetActive(!wasUnlocked);

        // Débloque exclusivement via le SaveManager.
        if (!wasUnlocked)
        {
            manager.UnlockSticker(sticker);

            manager.AddAchievementProgress(
                AchievementType.stickers20,
                1
            );
        }

        // Le SaveManager est désormais l'unique source de vérité.
        if (spr != null)
            spr.sprite = sticker.sticker_sprite;

        if (name_ != null)
            name_.text = sticker.sticker_name.GetLocalizedString();

        if (rarity != null)
        {
            switch (sticker.rarity)
            {
                case 0:
                    rarity.text = common.GetLocalizedString();
                    PlayRaritySound(common_sfx);
                    break;

                case 1:
                    rarity.text = rare.GetLocalizedString();
                    PlayRaritySound(rare_sfx);
                    break;

                case 2:
                    rarity.text = epic.GetLocalizedString();
                    PlayRaritySound(rare_sfx);
                    break;

                default:
                    rarity.text = "";
                    break;
            }
        }
    }

    private void PlayRaritySound(AudioClip clip)
    {
        if (source != null && clip != null)
            source.PlayOneShot(clip);
    }

    // --------------------------------------------------
    // DISPLAY ITEM
    // --------------------------------------------------

    private void DisplayItem(SO_shop_item1 item)
    {
        if (item == null)
            return;

        if (spr != null)
            spr.sprite = item.item_sprite;

        if (name_ != null)
            name_.text = item.item_name.GetLocalizedString();

        if (rarity != null)
            rarity.text = "";

        if (new_ != null)
            new_.SetActive(false);
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

        bool hasNext;

        if (showingStickers)
        {
            hasNext = stickerQueue != null &&
                      currentIndex < stickerQueue.Length;
        }
        else
        {
            hasNext = itemQueue != null &&
                      currentIndex < itemQueue.Length;
        }

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
        else
        {
            Close();
        }
    }

    private void AllowNext()
    {
        if (!isClosing)
            can_quit = true;
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

        if (SceneManager.GetActiveScene().name == "Achievements")
        {
            if (SC_ConstellationNavigation.instance != null)
                SC_ConstellationNavigation.instance.enabled = true;

            if (SC_achievement_menu.instance != null)
            {
                SC_achievement_menu.instance.caninteract = true;
                SC_achievement_menu.instance.show_sticker();
                SC_achievement_menu.instance.check_constellation();
            }
        }

        if (SC_music_manager.instance != null)
            SC_music_manager.instance.resume_music();

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

        if (SceneManager.GetActiveScene().name == "Shop")
        {
            if (SC_shop_manager.instance != null)
            {
                if (SC_money_shop.instance != null)
                {
                    SC_money_shop.instance.Buy(
                        SC_money_shop.instance.future_price
                    );
                }

                SC_shop_manager.instance.Talk_();
            }
        }

        gameObject.SetActive(false);

        isClosing = false;
        stickerQueue = null;
        itemQueue = null;
        currentIndex = 0;
    }
}
