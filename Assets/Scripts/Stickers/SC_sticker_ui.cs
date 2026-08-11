using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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
    public SC_alpha_cut_button cut;

    [Header("Scale")]
    public float defaultScale = 1f;
    public float minScale = 0.5f;
    public float maxScale = 2.5f;
    public float wheelSpeed = 0.2f;
    public float buttonScaleSpeed = 0.05f;

    [Header("Rotation")]
    public float rotationSpeed = 10f;
    public float buttonRotationSpeed = 10f;

    [Header("Balatro Card Effect")]
    [Tooltip("Inclinaison maximale sur X/Y.")]
    public float maxTilt = 12f;

    [Tooltip("Sensibilit? du tilt par rapport ? la vitesse de d?placement.")]
    public float tiltSensitivity = 0.08f;

    [Tooltip("Vitesse ? laquelle le tilt suit le mouvement.")]
    public float tiltSmoothSpeed = 12f;

    [Tooltip("Vitesse de retour ? plat quand la carte s'arr?te.")]
    public float tiltReturnSpeed = 8f;

    [Tooltip("Vitesse maximale prise en compte pour le tilt.")]
    public float maxTiltVelocity = 1000f;

    [Header("Balatro Scale Punch")]
    [Tooltip("Petit agrandissement pendant le d?placement.")]
    public float dragScaleMultiplier = 1.02f;

    [Tooltip("Vitesse d'application du scale pendant le drag.")]
    public float dragScaleSmoothSpeed = 8f;

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

    // =========================================================
    // ROTATION UTILISATEUR
    // =========================================================

    // Rotation Z r?elle du sticker.
    // Le tilt Balatro ne touche jamais cette valeur.
    private float stickerRotationZ;

    // =========================================================
    // BALATRO TILT
    // =========================================================

    private Vector2 lastDragPosition;
    private Vector2 dragVelocity;

    private float currentTiltX;
    private float currentTiltY;

    // Scale de base contr?l? par le syst?me de scale.
    private float stickerScale;

    // =========================================================
    // AWAKE
    // =========================================================
    public void SetSavedScale(float scaleX, float scaleY)
    {
        float absScale = Mathf.Abs(scaleX);

        stickerScale = Mathf.Clamp(
            absScale,
            minScale,
            maxScale
        );

        rect.localScale = new Vector3(
            scaleX,
            scaleY,
            1f
        );
    }
    void Awake()
    {
        rect = GetComponent<RectTransform>();

        canvas = GetComponentInParent<Canvas>();

        img = GetComponent<Image>();

        baseColor = img.color;

        // -----------------------------------------------------
        // SCALE INITIAL
        // -----------------------------------------------------

        stickerScale = defaultScale;

        rect.localScale =
            Vector3.one * stickerScale;

        // -----------------------------------------------------
        // ROTATION INITIAL
        // -----------------------------------------------------

        stickerRotationZ =
            rect.localEulerAngles.z;

        // -----------------------------------------------------
        // ALPHA HIT TEST
        // -----------------------------------------------------

        img.alphaHitTestMinimumThreshold = 0.1f;

        // -----------------------------------------------------
        // MATERIAL UNIQUE
        // -----------------------------------------------------

        if (img.material != null)
            img.material = new Material(img.material);
    }


    // =========================================================
    // ENABLE
    // =========================================================

    void OnEnable()
    {
        EnableAction(click);
        EnableAction(deleteSticker);

        EnableAction(mouseScroll);
        EnableAction(rotateModifier);

        EnableAction(scalePlus);
        EnableAction(scaleMinus);

        EnableAction(rotatePlus);
        EnableAction(rotateMinus);

        EnableAction(flip);
    }


    void EnableAction(InputActionReference action)
    {
        if (action != null && action.action != null)
            action.action.Enable();
    }


    void DisableAction(InputActionReference action)
    {
        if (action != null && action.action != null)
            action.action.Disable();
    }


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        cursor =
            SC_scursorManager.instance;

        deleteZone =
            GameObject.Find("TrashZone")
            .GetComponent<RectTransform>();


        // Si le sticker vient d'?tre cr??,
        // il commence directement en mode drag.
        if (!spawnedSticker)
        {
            dragging = true;

            offset = Vector2.zero;

            cursor.SetGrabCursor();

            lastDragPosition =
                rect.anchoredPosition;
        }


        selected.SetActive(true);

        anim.enabled = true;
    }


    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        if (SceneManager.GetActiveScene().name != "Stickers")
            return;


        // =====================================================
        // DELETE
        // =====================================================

        if (IsReleased(deleteSticker) && isHovered)
        {
            DeleteSticker();
            return;
        }


        // =====================================================
        // FLIP
        // =====================================================

        if (IsPressed(flip) )
        {
            if (dragging)
                FlipSticker();
        }


        // =====================================================
        // DRAG
        // =====================================================

        if (dragging)
        {
            HandleScaleRotation();

            HandleDrag();

            HandleBalatroEffect();

            CheckDeleteZone();
        }
        else
        {
            // M?me hors drag, on remet progressivement
            // le tilt ? z?ro.
            ResetBalatroTilt();
        }


        // =====================================================
        // CLICK
        // =====================================================

            if (IsPressed(click) )
            {
                ToggleDrag();
            }
    }


    // =========================================================
    // DRAG
    // =========================================================

    void HandleDrag()
    {
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


        Vector2 newPosition =
            local + offset;


        // -----------------------------------------------------
        // CALCUL DE LA VITESSE
        // -----------------------------------------------------

        if (Time.deltaTime > 0f)
        {
            dragVelocity =
                (newPosition - rect.anchoredPosition)
                / Time.deltaTime;
        }


        // -----------------------------------------------------
        // POSITION
        // -----------------------------------------------------

        rect.anchoredPosition =
            newPosition;
    }


    // =========================================================
    // BALATRO EFFECT
    // =========================================================

    void HandleBalatroEffect()
    {
        if (!dragging)
        {
            ResetBalatroTilt();
            return;
        }


        // -----------------------------------------------------
        // VITESSE
        // -----------------------------------------------------

        Vector2 velocity =
            Vector2.ClampMagnitude(
                dragVelocity,
                maxTiltVelocity
            );


        // -----------------------------------------------------
        // TARGET TILT
        // -----------------------------------------------------

        // D?placement horizontal
        // => rotation Y
        //
        // D?placement vertical
        // => rotation X

        float targetTiltY =
            velocity.x *
            tiltSensitivity;

        float targetTiltX =
            -velocity.y *
            tiltSensitivity;


        targetTiltX =
            Mathf.Clamp(
                targetTiltX,
                -maxTilt,
                maxTilt
            );


        targetTiltY =
            Mathf.Clamp(
                targetTiltY,
                -maxTilt,
                maxTilt
            );


        // -----------------------------------------------------
        // SMOOTH TILT
        // -----------------------------------------------------

        float smooth =
            1f -
            Mathf.Exp(
                -tiltSmoothSpeed *
                Time.deltaTime
            );


        currentTiltX =
            Mathf.Lerp(
                currentTiltX,
                targetTiltX,
                smooth
            );


        currentTiltY =
            Mathf.Lerp(
                currentTiltY,
                targetTiltY,
                smooth
            );


        // -----------------------------------------------------
        // SCALE PUNCH
        // -----------------------------------------------------

        float targetScale =
            stickerScale *
            dragScaleMultiplier;


        float currentAbsScale =
            Mathf.Abs(rect.localScale.x);


        float scaleSmooth =
            1f -
            Mathf.Exp(
                -dragScaleSmoothSpeed *
                Time.deltaTime
            );


        float visualScale =
            Mathf.Lerp(
                currentAbsScale,
                targetScale,
                scaleSmooth
            );


        // -----------------------------------------------------
        // CONSERVE LE FLIP
        // -----------------------------------------------------

        float flipX =
            Mathf.Sign(rect.localScale.x);

        float flipY =
            Mathf.Sign(rect.localScale.y);


        rect.localScale =
            new Vector3(
                flipX * visualScale,
                flipY * visualScale,
                1f
            );


        // -----------------------------------------------------
        // ROTATION VISUELLE
        // -----------------------------------------------------

        // IMPORTANT :
        // stickerRotationZ reste la vraie rotation
        // d?finie par l'utilisateur.
        //
        // currentTiltX/Y sont uniquement visuels.

        rect.localRotation =
            Quaternion.Euler(
                currentTiltX,
                currentTiltY,
                stickerRotationZ
            );
    }


    // =========================================================
    // RESET BALATRO
    // =========================================================

    void ResetBalatroTilt()
    {
        float smooth =
            1f -
            Mathf.Exp(
                -tiltReturnSpeed *
                Time.deltaTime
            );


        currentTiltX =
            Mathf.Lerp(
                currentTiltX,
                0f,
                smooth
            );


        currentTiltY =
            Mathf.Lerp(
                currentTiltY,
                0f,
                smooth
            );


        // -----------------------------------------------------
        // SCALE RETOUR ? LA VALEUR NORMALE
        // -----------------------------------------------------

        float currentAbsScale =
            Mathf.Abs(rect.localScale.x);


        float scaleSmooth =
            1f -
            Mathf.Exp(
                -dragScaleSmoothSpeed *
                Time.deltaTime
            );


        float visualScale =
            Mathf.Lerp(
                currentAbsScale,
                stickerScale,
                scaleSmooth
            );


        float flipX =
            Mathf.Sign(rect.localScale.x);

        float flipY =
            Mathf.Sign(rect.localScale.y);


        rect.localScale =
            new Vector3(
                flipX * visualScale,
                flipY * visualScale,
                1f
            );


        // -----------------------------------------------------
        // ROTATION
        // -----------------------------------------------------

        rect.localRotation =
            Quaternion.Euler(
                currentTiltX,
                currentTiltY,
                stickerRotationZ
            );
    }


    // =========================================================
    // DRAG TOGGLE
    // =========================================================

    void ToggleDrag()
    {
        
            if (!dragging && isHovered && !SC_scursorManager.instance.grabing)
                StartDrag();
        
   

            else if(dragging)
                StopDrag();
    }


    // =========================================================
    // START DRAG
    // =========================================================

    void StartDrag()
    {
        cut.uncut();
                SC_sticker_menu.instance.start_edit_mode();

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


        // Reset vitesse
        dragVelocity = Vector2.zero;

        lastDragPosition =
            rect.anchoredPosition;


        img.maskable = false;


        rect.parent.SetAsLastSibling();


        anim.ResetTrigger("drop");
        anim.SetTrigger("grab");


        cursor.SetGrabCursor();
    }


    // =========================================================
    // STOP DRAG
    // =========================================================

    void StopDrag()
    {
        cut.cut();

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


        // -----------------------------------------------------
        // RESET TILT
        // -----------------------------------------------------

        dragVelocity = Vector2.zero;


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


        // =====================================================
        // MOUSE WHEEL
        // =====================================================

        Vector2 scroll =
            mouseScroll.action.ReadValue<Vector2>();


        if (Mathf.Abs(scroll.y) > 0.01f)
        {
            // Rotation avec CTRL / modifier
            if (IsHeld(rotateModifier))
            {
                RotateSticker(
                    -scroll.y * rotationSpeed
                );
            }

            // Sinon scale
            else
            {
                ScaleSticker(
                    scroll.y * wheelSpeed
                );
            }
        }


        // =====================================================
        // SCALE BUTTONS
        // =====================================================

        if (IsHeld(scalePlus))
        {
            ScaleSticker(
                buttonScaleSpeed *
                Time.deltaTime
            );
        }


        if (IsHeld(scaleMinus))
        {
            ScaleSticker(
                -buttonScaleSpeed *
                Time.deltaTime
            );
        }


        // =====================================================
        // ROTATION BUTTONS
        // =====================================================

        if (IsHeld(rotatePlus))
        {
            RotateSticker(
                buttonRotationSpeed *
                Time.deltaTime
            );
        }


        if (IsHeld(rotateMinus))
        {
            RotateSticker(
                -buttonRotationSpeed *
                Time.deltaTime
            );
        }
    }


    // =========================================================
    // SCALE
    // =========================================================

    void ScaleSticker(float amount)
    {
        // On utilise notre valeur de scale r?elle
        // plut?t que la valeur visuelle affect?e
        // par le Balatro effect.

        stickerScale += amount;


        stickerScale =
            Mathf.Clamp(
                stickerScale,
                minScale,
                maxScale
            );


        // -----------------------------------------------------
        // CONSERVE LE FLIP
        // -----------------------------------------------------

        float flipX =
            Mathf.Sign(rect.localScale.x);

        float flipY =
            Mathf.Sign(rect.localScale.y);


        rect.localScale =
            new Vector3(
                flipX * stickerScale,
                flipY * stickerScale,
                1f
            );
    }


    // =========================================================
    // ROTATION
    // =========================================================

    void RotateSticker(float amount)
    {
        stickerRotationZ += amount;


        // On applique imm?diatement la rotation,
        // tout en conservant le tilt X/Y.

        rect.localRotation =
            Quaternion.Euler(
                currentTiltX,
                currentTiltY,
                stickerRotationZ
            );
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


        if (!dragging)
            SC_StickerSaveSystem.instance.AutoSave();
    }


    // =========================================================
    // INPUT HELPER
    // =========================================================

    bool IsHeld(InputActionReference action)
    {
        return action != null &&
               action.action != null &&
               action.action.IsPressed();
    }


    bool IsPressed(InputActionReference action)
    {
        return action != null &&
               action.action != null &&
               action.action.WasPressedThisFrame();
    }

    bool IsReleased(InputActionReference action)
    {
        return action != null &&
               action.action != null &&
               action.action.WasReleasedThisFrame();
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
    // POINTER ENTER
    // =========================================================

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        isHovered = true;


        if (!dragging)
            cursor.SetHoverCursor();
    }


    // =========================================================
    // POINTER EXIT
    // =========================================================

    public void OnPointerExit(
        PointerEventData eventData)
    {
        isHovered = false;


        if (!dragging)
            cursor.SetNormalCursor();
    }
}
