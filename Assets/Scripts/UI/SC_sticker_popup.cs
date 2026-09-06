using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;

public class SC_sticker_popup : MonoBehaviour
{
    public static SC_sticker_popup instance;

    public TextMeshPro name_;
    public TextMeshPro rarity;
    public SpriteRenderer spr;

    public LocalizedString common;
    public LocalizedString rare;
    public LocalizedString epic;

    public InputActionReference submit;
    public InputActionReference submit2;
    public Animator anim;

    public bool can_quit;

    private SO_Sticker[] stickerQueue;
    private int currentStickerIndex = 0;
    private bool isShowingSequence = false;
    private bool isClosing = false;

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
        Invoke(nameof(Delay), 0.5f);

        if (anim.enabled)
        {
            anim.ResetTrigger("hide");
            anim.ResetTrigger("get");
            anim.SetTrigger("show");
        }
        else
        {
            anim.enabled = true;
        }
        if (SceneManager.GetActiveScene().name == "Shop")
        {
            SC_shop_manager.instance.canquit = false;
        }
    }

    private void Update()
    {
        if (!isShowingSequence || isClosing)
            return;

        if (submit.action.WasPressedThisFrame() && can_quit)
        {
            ShowNextSticker();
        }

        if (submit2.action.WasPressedThisFrame() && can_quit)
        {
            ShowNextSticker();
        }
    }

    private void Delay()
    {
        can_quit = true;
    }

    // Appelé pour UNE récompense
    public void update_visuals(SO_Sticker sticker)
    {
        if (sticker == null)
            return;

        stickerQueue = new SO_Sticker[1];
        stickerQueue[0] = sticker;

        currentStickerIndex = 0;
        isShowingSequence = true;
        isClosing = false;

        gameObject.SetActive(true);

        DisplaySticker(stickerQueue[0]);
    }

    // Appelé pour PLUSIEURS récompenses
    public void ShowStickers(SO_Sticker[] stickersToShow)
    {
        if (stickersToShow == null || stickersToShow.Length == 0)
            return;

        stickerQueue = stickersToShow;
        currentStickerIndex = 0;
        isShowingSequence = true;
        isClosing = false;
        can_quit = false;

        gameObject.SetActive(true);

        DisplaySticker(stickerQueue[0]);
    }

    private void DisplaySticker(SO_Sticker sticker)
    {
        if (sticker == null)
            return;

        spr.sprite = sticker.sticker_sprite;
        name_.text = sticker.sticker_name;

        if (sticker.rarity == 0)
        {
            rarity.text = common.GetLocalizedString();
        }
        else if (sticker.rarity == 1)
        {
            rarity.text = rare.GetLocalizedString();
        }
        else if (sticker.rarity == 2)
        {
            rarity.text = epic.GetLocalizedString();
        }
    }

    private void ShowNextSticker()
    {
        if (isClosing)
            return;

        can_quit = false;

        currentStickerIndex++;

        // Il reste des stickers
        if (currentStickerIndex < stickerQueue.Length)
        {
            DisplaySticker(stickerQueue[currentStickerIndex]);

            anim.ResetTrigger("get");
            anim.SetTrigger("get");

            Invoke(nameof(AllowNext), 0.5f);
        }
        else
        {
            // Plus de stickers
            Close();
        }
    }

    private void AllowNext()
    {
        if (!isClosing)
            can_quit = true;
    }

    public void Close()
    {
        if (!isShowingSequence || isClosing)
            return;
        if (SceneManager.GetActiveScene().name == "Shop")
        {
            SC_shop_manager.instance.show_menu();
        }
        isClosing = true;
        isShowingSequence = false;
        can_quit = false;

        CancelInvoke();

        anim.ResetTrigger("get");
        anim.ResetTrigger("show");
        anim.SetTrigger("hide");

        // On laisse l'animation hide se terminer avant de désactiver le GameObject
        StartCoroutine(DisableAfterHide());
    }

    private IEnumerator DisableAfterHide()
    {
        // À adapter à la durée réelle de ton animation "hide"
        yield return new WaitForSeconds(0.5f);

   

        gameObject.SetActive(false);
        isClosing = false;
    }
}
