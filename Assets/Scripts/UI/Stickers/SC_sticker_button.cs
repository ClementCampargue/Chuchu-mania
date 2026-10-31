
using UnityEngine;
using UnityEngine.UI;

public class SC_sticker_button : MonoBehaviour
{
    [Header("Sticker")]
    public SpriteRenderer spriteRenderer;
    public SO_Sticker sticker;
    public Material locked;
    public GameObject Sticker_prefab;

    [Header("Button")]
    public SC_Button button;
    public Animator anim;

    [Header("Juiciness")]
    public SC_juiciness juice;
    public SC_juiciness juice2;

    private SC_SaveManager saveManager;
    private SC_StickerSaveSystem save;
    private SC_sticker_menu menu;

    private void Start()
    {
        saveManager = SC_SaveManager.instance;
        save = SC_StickerSaveSystem.instance;
        menu = SC_sticker_menu.instance;

        spriteRenderer.sprite = sticker.sticker_sprite;

        UpdateAppearance();

     
    }
    private void Update()
    {
        if (!menu.editing)
        {
            button.enabled = true;
            button.accept_input = true;
            button.clickable = true;
        }
        else
        {
            button.isSelected = false;
            button.isHovered = false;
            button.enabled = false;
            button.accept_input = false;
            button.clickable = false;
        }
    }
    private void UpdateAppearance()
    {
        if (IsUnlocked())
        {
            spriteRenderer.material = sticker.special_mat != null
                ? sticker.special_mat
                : spriteRenderer.material;
        }
        else
        {
            spriteRenderer.material = locked;
        }
    }

    public bool IsUnlocked()
    {
        if (saveManager == null)
            saveManager = SC_SaveManager.instance;

        if (saveManager == null)
            return false;

        return saveManager.IsStickerUnlocked(sticker);
    }

    public void unhover()
    {
    }

    public void update_infos()
    {
        if (juice != null)
            juice.PlayJuice();

        menu.update_infos(sticker, null);
    }

    public void CreateSticker()
    {
        if (!IsUnlocked())
            return;

        if (anim != null)
            anim.SetTrigger("Press");

        SC_scursorManager.instance.grabing = true;

        if (juice2 != null)
            juice2.PlayJuice();

        menu.start_edit_mode();
        SC_scursorManager.instance.enable_cursor();
        menu.StartEditMode();
        if (button != null)
            button.UnSelect();

        GameObject obj = Instantiate(
            Sticker_prefab,
            save.parent
        ).transform.GetChild(0).gameObject;

        obj.GetComponent<SC_sticker_UI>().spawnedSticker = true;

        // Positionnement du sticker à la position de la souris
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(
            Input.mousePosition
        );

        mousePosition.z = 0f;

        obj.transform.position = mousePosition;

        // Application du sprite
        Image sr = obj.GetComponent<Image>();

        if (sr != null)
        {
            sr.sprite = sticker.sticker_sprite;

            if (sticker.special_mat != null)
                sr.material = sticker.special_mat;
        }
    }

   
}
