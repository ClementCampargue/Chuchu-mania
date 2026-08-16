using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SC_sticker_button : MonoBehaviour
{
    public Image image;

    public SO_Sticker sticker;
    public Material locked;
    public Material unlocked;
    public Button button;
    public GameObject Sticker_prefab;

    public InputActionReference selectAction; // <-- Ajoute ça dans l'inspecteur

    private bool selected;

    private SC_StickerSaveSystem save;
    private SC_sticker_menu menu;
    public Animator anim;

    public SC_juiciness juice;
    public SC_juiciness juice2;

    private void Start()
    {
        save = SC_StickerSaveSystem.instance;
        menu = SC_sticker_menu.instance;
        image.material = sticker.unlocked ? unlocked : locked;
        if(sticker.special_mat != null)
        {
            image.material = sticker.special_mat;
        }
        image.sprite = sticker.sticker_sprite;
        unlocked = image.material;
    }


    private void OnEnable()
    {
        selectAction.action.Enable();

        image.sprite = sticker.sticker_sprite;
        unlocked = image.material;

        image.material = sticker.unlocked ? unlocked : locked;

    }



    private void Update()
    {
        if (button != null)
        {
            button.image.raycastTarget = !SC_controller_manager.instance.using_controller;
        }

   

        if (selected && selectAction.action.WasPressedThisFrame())
        {
            anim.SetTrigger("Press");
            CreateSticker();
        }
    }

    public void unhover()
    {


        selected = false;
    }

    public void update_infos()
    {

        selected = true;
        juice.PlayJuice();
       

        menu.update_infos(sticker, button);
    }


    public void CreateSticker()
    {
        if (!sticker.unlocked)
            return;
        SC_scursorManager.instance.grabing = true;
        juice2.PlayJuice();
        menu.start_edit_mode();
        SC_scursorManager.instance.enable_cursor();
        // Désactive toute sélection/navigation UI
        EventSystem.current.SetSelectedGameObject(null);

        Canvas canvas = GameObject.Find("MainCanvas").GetComponent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        GameObject obj = Instantiate(
            Sticker_prefab,
            save.parent
        ).transform.GetChild(0).gameObject;
        obj.GetComponent<SC_sticker_UI>().spawnedSticker = true;
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

        if (sticker.special_mat != null)
        {
            img.material = sticker.special_mat;
        }

        img.maskable = false;
    }
}