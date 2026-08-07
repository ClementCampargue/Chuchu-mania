using UnityEngine;
using UnityEngine.UI;

public class SC_sticker_button : MonoBehaviour
{

    public Image image;

    public SO_Sticker sticker;
    public Material locked;
    private Material unlocked;
    public Button button;
    public GameObject Sticker_prefab;

    private SC_StickerSaveSystem save;
    private SC_sticker_menu menu;

    private void Start()
    {
        save = SC_StickerSaveSystem.instance;
        menu = SC_sticker_menu.instance;

        image.sprite = sticker.sticker_sprite;
        unlocked = image.material;

    }


    private void OnEnable()
    {
        image.sprite = sticker.sticker_sprite;
        unlocked = image.material;

        image.material = sticker.unlocked ? unlocked : locked;
    }


    private void Update()
    {
        image.material = sticker.unlocked ? unlocked : locked;
    }


    public void update_infos()
    {
        menu.update_infos(sticker);
    }
    public void CreateSticker()
    {
        if (!sticker.unlocked)
            return;

        menu.start_edit_mode();
        Canvas canvas = GameObject.Find("MainCanvas").GetComponent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        GameObject obj = Instantiate(
            Sticker_prefab,
            save.parent
        ).transform.GetChild(0).gameObject;

        Image img = obj.GetComponent<Image>();
        RectTransform rect = obj.GetComponent<RectTransform>();


        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Input.mousePosition,
            canvas.worldCamera,
            out Vector2 localPoint
        );

        rect.anchoredPosition = localPoint;

        img.sprite = sticker.sticker_sprite;
        img.maskable = false;
    }
}