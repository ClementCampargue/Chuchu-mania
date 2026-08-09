using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class SC_sticker_UI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    private bool isHovered;

    [Header("Input Actions")]
    public InputActionReference mouseScroll;
    public InputActionReference rotateModifier;
    public InputActionReference click;
    public InputActionReference deleteSticker;

    public InputActionReference scalePlus;
    public InputActionReference scaleMinus;
    public InputActionReference rotatePlus;
    public InputActionReference rotateMinus;

    // Flip du sticker
    public InputActionReference flip;


    [Header("Scale")]
    public float defaultScale = 1f;
    public float minScale = 0.5f;
    public float maxScale = 2.5f;
    public float wheelSpeed = 0.2f;
    public float buttonScaleSpeed = 0.05f;


    [Header("Rotation")]
    public float rotationSpeed = 10f;
    public float buttonRotationSpeed = 10f;


    [Header("UI")]
    public Image img;
    public GameObject selected;
    public Animator anim;


    [Header("Delete")]
    public RectTransform deleteZone;


    public bool spawnedSticker;

    private RectTransform rect;
    private Canvas canvas;

    private bool dragging;
    private bool overDeleteZone;

    private Vector2 offset;

    private Color baseColor;

    private SC_scursorManager cursor;


    void Awake()
    {
        rect = GetComponent<RectTransform>();

        canvas = GetComponentInParent<Canvas>();

        img = GetComponent<Image>();

        baseColor = img.color;

        // Scale initial
        rect.localScale =
            Vector3.one * defaultScale;

        // Permet de détecter uniquement les pixels visibles
        img.alphaHitTestMinimumThreshold = 0.1f;

        // Copie du material pour éviter de modifier
        // le material partagé par les autres stickers
        if (img.material != null)
            img.material =
                new Material(img.material);
    }


    void OnEnable()
    {
        click.action.Enable();
        deleteSticker.action.Enable();

        mouseScroll.action.Enable();
        rotateModifier.action.Enable();

        scalePlus.action.Enable();
        scaleMinus.action.Enable();

        rotatePlus.action.Enable();
        rotateMinus.action.Enable();

        // Flip
        flip.action.Enable();
    }


    void Start()
    {
        cursor =
            SC_scursorManager.instance;


        deleteZone =
            GameObject.Find("TrashZone")
            .GetComponent<RectTransform>();


        // Si le sticker vient d'être créé,
        // il commence directement en mode drag
        if (!spawnedSticker)
        {
            dragging = true;

            offset = Vector2.zero;

            cursor.SetGrabCursor();
        }


        selected.SetActive(true);

        anim.enabled = true;
    }


    void Update()
    {
        if (SceneManager.GetActiveScene().name != "Stickers")
            return;


        // =========================
        // DELETE
        // =========================

        if (deleteSticker.action.WasPressedThisFrame() && isHovered)
        {
            DeleteSticker();
        }


        // =========================
        // FLIP
        // =========================

        if (flip.action.WasPressedThisFrame() && isHovered)
        {
            FlipSticker();
        }


        // =========================
        // DRAG
        // =========================

        if (dragging)
        {
            HandleScaleRotation();


            Vector2 screenPos =
                RectTransformUtility.WorldToScreenPoint(
                    canvas.worldCamera,
                    cursor.transform.position
                );


            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                screenPos,
                canvas.worldCamera,
                out Vector2 local
            );


            rect.anchoredPosition =
                local + offset;


            CheckDeleteZone();
        }


        // =========================
        // CLICK
        // =========================

        if (click.action.WasPressedThisFrame() && isHovered)
        {
            ToggleDrag();
        }
    }


    // =========================================================
    // DRAG
    // =========================================================

    void ToggleDrag()
    {
        if (!dragging)
            StartDrag();

        else
            StopDrag();
    }


    void StartDrag()
    {
        dragging = true;

        selected.SetActive(true);


        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            cursor.transform.position,
            canvas.worldCamera,
            out Vector2 local
        );


        offset =
            rect.anchoredPosition - local;


        img.maskable = false;


        rect.parent.SetAsLastSibling();


        anim.ResetTrigger("drop");
        anim.SetTrigger("grab");


        SC_sticker_menu.instance.start_edit_mode();


        cursor.SetGrabCursor();
    }


    void StopDrag()
    {
        dragging = false;


        selected.SetActive(false);


        img.maskable = true;


        bool delete =
            RectTransformUtility.RectangleContainsScreenPoint(
                deleteZone,
                cursor.transform.position,
                canvas.worldCamera
            );


        if (delete)
        {
            DeleteSticker();
            return;
        }


        anim.ResetTrigger("grab");
        anim.SetTrigger("drop");


        SC_sticker_menu.instance.quit_edit_mode();


        SC_StickerSaveSystem.instance.AutoSave();


        cursor.SetHoverCursor();
    }


    // =========================================================
    // SCALE + ROTATION
    // =========================================================

    void HandleScaleRotation()
    {
        if (!dragging)
            return;


        Vector2 scroll =
            mouseScroll.action.ReadValue<Vector2>();


        if (Mathf.Abs(scroll.y) < 0.01f)
            return;


        // =========================
        // ROTATION
        // =========================

        if (rotateModifier.action.IsPressed())
        {
            rect.Rotate(
                0,
                0,
                -scroll.y * rotationSpeed
            );
        }


        // =========================
        // SCALE
        // =========================

        else
        {
            // On récupère la valeur absolue
            // pour ne pas casser le scale
            // lorsque le sticker est retourné.
            float scale =
                Mathf.Abs(rect.localScale.x);


            scale +=
                scroll.y * wheelSpeed;


            scale =
                Mathf.Clamp(
                    scale,
                    minScale,
                    maxScale
                );


            // On conserve le flip horizontal
            float flipX =
                Mathf.Sign(rect.localScale.x);

            // On conserve également le flip vertical
            float flipY =
                Mathf.Sign(rect.localScale.y);


            rect.localScale =
                new Vector3(
                    flipX * scale,
                    flipY * scale,
                    1f
                );
        }
    }


    // =========================================================
    // FLIP
    // =========================================================

    void FlipSticker()
    {
        Vector3 scale =
            rect.localScale;


        // Inverse le sens horizontal
        scale.x *= -1f;


        rect.localScale =
            scale;
        SC_StickerSaveSystem.instance.AutoSave();
    }


    // =========================================================
    // DELETE ZONE
    // =========================================================

    void CheckDeleteZone()
    {
        if (deleteZone == null)
            return;


        overDeleteZone =
            RectTransformUtility.RectangleContainsScreenPoint(
                deleteZone,
                cursor.transform.position,
                canvas.worldCamera
            );


        if (overDeleteZone)
        {
            float alpha =
                Mathf.Lerp(
                    0.3f,
                    1f,
                    (Mathf.Sin(Time.time * 12f) + 1f) / 2f
                );


            img.color =
                new Color(
                    baseColor.r,
                    baseColor.g,
                    baseColor.b,
                    alpha
                );
        }
        else
        {
            img.color =
                baseColor;
        }
    }


    // =========================================================
    // DELETE
    // =========================================================

    void DeleteSticker()
    {
        SC_sticker_menu.instance.quit_edit_mode();


        SC_StickerSaveSystem.instance.AutoSave();


        cursor.SetNormalCursor();


        Destroy(transform.parent.gameObject);
        SC_StickerSaveSystem.instance.AutoSave();
    }


    // =========================================================
    // POINTER
    // =========================================================

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        isHovered = true;


        if (!dragging)
            cursor.SetHoverCursor();
    }


    public void OnPointerExit(
        PointerEventData eventData)
    {
        isHovered = false;


        if (!dragging)
            cursor.SetNormalCursor();
    }
}
