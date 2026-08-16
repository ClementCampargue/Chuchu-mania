using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SC_sticker_UI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    // =========================================================
    // DEFAULT STATE
    // =========================================================

    private Vector2 defaultPosition;
    private float defaultRotationZ;
    private float defaultStickerScale;

    private bool defaultFlipX;
    private bool defaultFlipY;

    private bool isHovered;

    // =========================================================
    // INPUT ACTIONS
    // =========================================================

    [Header("Input Actions")]
    public InputActionReference mouseScroll;
    public InputActionReference rotateModifier;
    public InputActionReference click;
    public InputActionReference deleteSticker;

    public InputActionReference scalePlus;
    public InputActionReference scaleMinus;
    public InputActionReference rotatePlus;
    public InputActionReference rotateMinus;

    public InputActionReference flip;

    public SC_alpha_cut_button cut;

    // =========================================================
    // SCALE
    // =========================================================

    [Header("Scale")]
    public float defaultScale = 1f;
    public float minScale = 0.5f;
    public float maxScale = 2.5f;
    public float wheelSpeed = 0.2f;
    public float buttonScaleSpeed = 0.05f;

    // =========================================================
    // ROTATION
    // =========================================================

    [Header("Rotation")]
    public float rotationSpeed = 10f;
    public float buttonRotationSpeed = 10f;

    // =========================================================
    // BALATRO CARD EFFECT
    // =========================================================

    [Header("Balatro Card Effect")]
    [Tooltip("Inclinaison maximale sur X/Y.")]
    public float maxTilt = 12f;

    [Tooltip("Sensibilité du tilt par rapport à la vitesse de déplacement.")]
    public float tiltSensitivity = 0.08f;

    [Tooltip("Vitesse à laquelle le tilt suit le mouvement.")]
    public float tiltSmoothSpeed = 12f;

    [Tooltip("Vitesse de retour à plat quand la carte s'arrête.")]
    public float tiltReturnSpeed = 8f;

    [Tooltip("Vitesse maximale prise en compte pour le tilt.")]
    public float maxTiltVelocity = 1000f;

    // =========================================================
    // BALATRO SCALE
    // =========================================================

    [Header("Balatro Scale Punch")]
    [Tooltip("Petit agrandissement pendant le déplacement.")]
    public float dragScaleMultiplier = 1.02f;

    [Tooltip("Vitesse d'application du scale pendant le drag.")]
    public float dragScaleSmoothSpeed = 8f;

    // =========================================================
    // UI
    // =========================================================

    [Header("UI")]
    public Image img;
    public GameObject selected;
    public Animator anim;

    // =========================================================
    // DELETE
    // =========================================================

    [Header("Delete")]
    public RectTransform deleteZone;

    public bool spawnedSticker;

    // =========================================================
    // REFERENCES
    // =========================================================

    private RectTransform rect;
    private Canvas canvas;

    private bool dragging;
    private bool overDeleteZone;

    private Vector2 offset;

    private Color baseColor;

    private SC_scursorManager cursor;

    // =========================================================
    // REAL STICKER VALUES
    // =========================================================

    // Rotation Z réelle du sticker.
    // Le Balatro tilt ne modifie jamais cette valeur.
    private float stickerRotationZ;

    // Scale réel du sticker.
    // Le Balatro punch ne modifie jamais cette valeur.
    private float stickerScale;

    // Flip réel.
    private bool flipX;
    private bool flipY;

    // =========================================================
    // BALATRO TILT
    // =========================================================

    private Vector2 lastDragPosition;
    private Vector2 dragVelocity;

    private float currentTiltX;
    private float currentTiltY;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        rect = GetComponent<RectTransform>();

        canvas = GetComponentInParent<Canvas>();

        img = GetComponent<Image>();

        if (img != null)
        {
            baseColor = img.color;

            img.alphaHitTestMinimumThreshold = 0.1f;

            if (img.material != null)
            {
                img.material = new Material(img.material);
            }
        }

        // -----------------------------------------------------
        // INITIAL STATE
        // -----------------------------------------------------

        stickerScale = Mathf.Clamp(
            defaultScale,
            minScale,
            maxScale
        );

        stickerRotationZ =
            rect.localEulerAngles.z;

        flipX =
            rect.localScale.x < 0f;

        flipY =
            rect.localScale.y < 0f;

        if (Mathf.Approximately(rect.localScale.x, 0f))
            flipX = false;

        if (Mathf.Approximately(rect.localScale.y, 0f))
            flipY = false;

        rect.localScale =
            new Vector3(
                GetScaleSignX() * stickerScale,
                GetScaleSignY() * stickerScale,
                1f
            );

        // -----------------------------------------------------
        // SCENE
        // -----------------------------------------------------

        if (SceneManager.GetActiveScene().name != "Stickers")
        {
            spawnedSticker = false;
        }

        // -----------------------------------------------------
        // INPUT
        // -----------------------------------------------------

        EnableAction(rotateModifier);

        EnableAction(scalePlus);
        EnableAction(scaleMinus);

        EnableAction(rotatePlus);
        EnableAction(rotateMinus);

        EnableAction(flip);

        // IMPORTANT :
        // Aucun SaveDefaultState ici.
        //
        // Le SaveSystem peut être en train de charger
        // ce sticker. Awake() arrive AVANT les données
        // sauvegardées.
    }

    // =========================================================
    // INPUT ENABLE
    // =========================================================

    private void EnableAction(InputActionReference action)
    {
        if (action != null && action.action != null)
        {
            action.action.Enable();
        }
    }

    private void DisableAction(InputActionReference action)
    {
        if (action != null && action.action != null)
        {
            action.action.Disable();
        }
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        cursor =
            SC_scursorManager.instance;

        // -----------------------------------------------------
        // TRASH ZONE
        // -----------------------------------------------------

        if (deleteZone == null)
        {
            GameObject trash =
                GameObject.Find("TrashZone");

            if (trash != null)
            {
                deleteZone =
                    trash.GetComponent<RectTransform>();
            }
        }

        // -----------------------------------------------------
        // NOUVEAU STICKER SPAWNÉ PAR UN BOUTON
        // -----------------------------------------------------

        if (spawnedSticker)
        {
            // IMPORTANT :
            // À ce moment-là, le code qui a Instantiate()
            // le sticker a normalement déjà eu le temps de
            // définir sa position.
            //
            // On mémorise donc la vraie position de spawn,
            // et non (0,0).

            stickerScale =
                Mathf.Clamp(
                    Mathf.Abs(rect.localScale.x),
                    minScale,
                    maxScale
                );

            flipX =
                rect.localScale.x < 0f;

            flipY =
                rect.localScale.y < 0f;

            stickerRotationZ =
                rect.localEulerAngles.z;

            SaveDefaultState();

            // -------------------------------------------------
            // START DRAG
            // -------------------------------------------------

            dragging = true;

            if (SC_sticker_menu.instance != null)
            {
                SC_sticker_menu.instance
                    .SetDraggingSticker(this);
            }

            offset =
                Vector2.zero;

            if (cursor != null)
            {
                cursor.SetGrabCursor();
            }

            lastDragPosition =
                rect.anchoredPosition;
        }

        // -----------------------------------------------------
        // UI
        // -----------------------------------------------------

        if (selected != null)
        {
            selected.SetActive(true);
        }

        if (anim != null)
        {
            anim.enabled = true;
        }
    }
    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
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

        if (IsPressed(flip))
        {
            if (dragging)
            {
                FlipSticker();
            }
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
            ResetBalatroTilt();
        }

        // =====================================================
        // CLICK
        // =====================================================

        if (IsPressed(click))
        {
            ToggleDrag();
        }
    }

    // =========================================================
    // DRAG
    // =========================================================

    private void HandleDrag()
    {
        if (canvas == null || cursor == null)
            return;

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
        // VELOCITY
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

        lastDragPosition =
            newPosition;
    }

    // =========================================================
    // BALATRO EFFECT
    // =========================================================

    private void HandleBalatroEffect()
    {
        if (!dragging)
        {
            ResetBalatroTilt();
            return;
        }

        // -----------------------------------------------------
        // VELOCITY
        // -----------------------------------------------------

        Vector2 velocity =
            Vector2.ClampMagnitude(
                dragVelocity,
                maxTiltVelocity
            );

        // -----------------------------------------------------
        // TARGET TILT
        // -----------------------------------------------------

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
        // APPLY VISUAL SCALE
        // -----------------------------------------------------

        rect.localScale =
            new Vector3(
                GetScaleSignX() * visualScale,
                GetScaleSignY() * visualScale,
                1f
            );

        // -----------------------------------------------------
        // APPLY VISUAL ROTATION
        // -----------------------------------------------------

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

    private void ResetBalatroTilt()
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
        // SCALE RETURN
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

        rect.localScale =
            new Vector3(
                GetScaleSignX() * visualScale,
                GetScaleSignY() * visualScale,
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

    private void ToggleDrag()
    {
        if (!dragging &&
            isHovered &&
            SC_scursorManager.instance != null &&
            !SC_scursorManager.instance.grabing)
        {
            StartDrag();
        }
        else if (dragging)
        {
            StopDrag();
        }
    }

    // =========================================================
    // START DRAG
    // =========================================================

    private void StartDrag()
    {
        if (cut != null)
            cut.uncut();

        if (SC_sticker_menu.instance != null)
            SC_sticker_menu.instance.start_edit_mode();

        dragging = true;

        if (SC_sticker_menu.instance != null)
            SC_sticker_menu.instance.SetDraggingSticker(this);

        if (selected != null)
            selected.SetActive(true);

        if (canvas != null &&
            cursor != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                cursor.transform.position,
                canvas.worldCamera,
                out Vector2 local
            );

            offset =
                rect.anchoredPosition -
                local;
        }

        dragVelocity =
            Vector2.zero;

        lastDragPosition =
            rect.anchoredPosition;

        if (img != null)
            img.maskable = false;

        if (rect.parent != null)
            rect.parent.SetAsLastSibling();

        if (anim != null)
        {
            anim.ResetTrigger("drop");
            anim.SetTrigger("grab");
        }

        if (cursor != null)
            cursor.SetGrabCursor();
    }

    // =========================================================
    // STOP DRAG
    // =========================================================

    private void StopDrag()
    {
        SaveDefaultState();

        if (cut != null)
            cut.cut();

        dragging = false;

        // -----------------------------------------------------
        // RESET VISUAL BALATRO AVANT SAVE
        // -----------------------------------------------------

        currentTiltX = 0f;
        currentTiltY = 0f;

        rect.localScale =
            new Vector3(
                GetScaleSignX() * stickerScale,
                GetScaleSignY() * stickerScale,
                1f
            );

        rect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                stickerRotationZ
            );

        // -----------------------------------------------------
        // UI
        // -----------------------------------------------------

        if (selected != null)
            selected.SetActive(false);

        if (img != null)
            img.maskable = true;

        // -----------------------------------------------------
        // DELETE ZONE
        // -----------------------------------------------------

        bool delete = false;

        if (deleteZone != null &&
            cursor != null &&
            canvas != null)
        {
            delete =
                RectTransformUtility.RectangleContainsScreenPoint(
                    deleteZone,
                    cursor.transform.position,
                    canvas.worldCamera
                );
        }

        if (delete)
        {
            DeleteSticker();
            return;
        }

        // -----------------------------------------------------
        // RESET DRAG
        // -----------------------------------------------------

        dragVelocity =
            Vector2.zero;

        spawnedSticker = false;

        if (anim != null)
        {
            anim.ResetTrigger("grab");
            anim.SetTrigger("drop");
        }

        if (SC_sticker_menu.instance != null)
            SC_sticker_menu.instance.quit_edit_mode();

        // -----------------------------------------------------
        // SAVE
        // -----------------------------------------------------

        if (SC_StickerSaveSystem.instance != null)
        {
            SC_StickerSaveSystem.instance.AutoSave();
        }

        if (cursor != null)
            cursor.SetHoverCursor();
    }

    // =========================================================
    // SCALE + ROTATION
    // =========================================================

    private void HandleScaleRotation()
    {
        if (!dragging)
            return;

        // =====================================================
        // MOUSE WHEEL
        // =====================================================

        if (mouseScroll != null &&
            mouseScroll.action != null)
        {
            Vector2 scroll =
                mouseScroll.action.ReadValue<Vector2>();

            if (Mathf.Abs(scroll.y) > 0.01f)
            {
                if (IsHeld(rotateModifier))
                {
                    RotateSticker(
                        -scroll.y * rotationSpeed
                    );
                }
                else
                {
                    ScaleSticker(
                        scroll.y * wheelSpeed
                    );
                }
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

    private void ScaleSticker(float amount)
    {
        stickerScale += amount;

        stickerScale =
            Mathf.Clamp(
                stickerScale,
                minScale,
                maxScale
            );

        rect.localScale =
            new Vector3(
                GetScaleSignX() * stickerScale,
                GetScaleSignY() * stickerScale,
                1f
            );
    }

    // =========================================================
    // ROTATION
    // =========================================================

    private void RotateSticker(float amount)
    {
        stickerRotationZ += amount;

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

    private void FlipSticker()
    {
        flipX = !flipX;

        float visualScale =
            Mathf.Abs(rect.localScale.x);

        rect.localScale =
            new Vector3(
                GetScaleSignX() * visualScale,
                GetScaleSignY() * visualScale,
                1f
            );

        // Si le sticker n'est pas en drag,
        // on sauvegarde immédiatement.
        if (!dragging &&
            SC_StickerSaveSystem.instance != null)
        {
            SC_StickerSaveSystem.instance.AutoSave();
        }
    }

    // =========================================================
    // SCALE SIGNS
    // =========================================================

    private float GetScaleSignX()
    {
        return flipX ? -1f : 1f;
    }

    private float GetScaleSignY()
    {
        return flipY ? -1f : 1f;
    }

    // =========================================================
    // INPUT HELPERS
    // =========================================================

    private bool IsHeld(InputActionReference action)
    {
        return action != null &&
               action.action != null &&
               action.action.IsPressed();
    }

    private bool IsPressed(InputActionReference action)
    {
        return action != null &&
               action.action != null &&
               action.action.WasPressedThisFrame();
    }

    private bool IsReleased(InputActionReference action)
    {
        return action != null &&
               action.action != null &&
               action.action.WasReleasedThisFrame();
    }

    // =========================================================
    // DELETE ZONE
    // =========================================================

    private void CheckDeleteZone()
    {
        if (deleteZone == null ||
            cursor == null ||
            canvas == null)
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

    private void DeleteSticker()
    {
        if (!SC_controller_manager.instance.using_controller)
        {
            if (SC_sticker_menu.instance != null)
                SC_sticker_menu.instance.quit_edit_mode();
        }
        if (spawnedSticker)
        {
            if (SC_sticker_menu.instance != null)
                SC_sticker_menu.instance.quit_edit_mode();
        }

        if (SC_StickerSaveSystem.instance != null)
            SC_StickerSaveSystem.instance.AutoSave();

        if (cursor != null)
            cursor.SetNormalCursor();

        // Le SC_sticker_UI peut être sur l'enfant
        // du prefab.
        //
        // On détruit donc le prefab parent.

        if (transform.parent != null)
        {
            Destroy(transform.parent.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if (SC_StickerSaveSystem.instance != null)
            SC_StickerSaveSystem.instance.AutoSave();
    }

    // =========================================================
    // POINTER ENTER
    // =========================================================

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        isHovered = true;

        if (!dragging &&
            cursor != null)
        {
            cursor.SetHoverCursor();
        }
    }

    // =========================================================
    // POINTER EXIT
    // =========================================================

    public void OnPointerExit(
        PointerEventData eventData)
    {
        isHovered = false;

        if (!dragging &&
            cursor != null)
        {
            cursor.SetNormalCursor();
        }
    }

    // =========================================================
    // DELETE IF DRAGGING
    // =========================================================

    public void DeleteIfDragging()
    {
        if (!dragging)
            return;

        if (!spawnedSticker)
        {
            ResetStickerToDefault();
        }
        else
        {
            DeleteSticker();
        }
        if (SC_StickerSaveSystem.instance != null)
        {
            SC_StickerSaveSystem.instance.AutoSave();
        }
    }

    // =========================================================
    // SAVE DEFAULT STATE
    // =========================================================
    //
    // Utilisé pour un nouveau sticker.
    //
    // IMPORTANT :
    // Cette fonction NE MODIFIE PAS le sticker.
    // Elle mémorise seulement son état actuel.
    // =========================================================

    public void SaveDefaultState()
    {
        defaultPosition =
            rect.anchoredPosition;

        defaultRotationZ =
            stickerRotationZ;

        defaultStickerScale =
            stickerScale;

        defaultFlipX =
            flipX;

        defaultFlipY =
            flipY;
    }

    // =========================================================
    // INITIALIZE LOADED STICKER
    // =========================================================
    //
    // Appelé par SC_StickerSaveSystem après Instantiate.
    //
    // C'est cette méthode qui synchronise complètement
    // le SC_sticker_UI avec les données sauvegardées.
    // =========================================================

    public void InitializeLoadedSticker(
        Vector2 position,
        float rotationZ,
        float scaleX,
        float scaleY)
    {
        // -----------------------------------------------------
        // POSITION
        // -----------------------------------------------------

        rect.anchoredPosition =
            position;

        // -----------------------------------------------------
        // SCALE
        // -----------------------------------------------------

        float absScale =
            Mathf.Clamp(
                Mathf.Abs(scaleX),
                minScale,
                maxScale
            );

        stickerScale =
            absScale;

        flipX =
            scaleX < 0f;

        flipY =
            scaleY < 0f;

        rect.localScale =
            new Vector3(
                GetScaleSignX() * stickerScale,
                GetScaleSignY() * stickerScale,
                1f
            );

        // -----------------------------------------------------
        // ROTATION
        // -----------------------------------------------------

        stickerRotationZ =
            rotationZ;

        rect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                stickerRotationZ
            );

        // -----------------------------------------------------
        // BALATRO RESET
        // -----------------------------------------------------

        currentTiltX = 0f;
        currentTiltY = 0f;

        dragVelocity =
            Vector2.zero;

        lastDragPosition =
            position;

        // -----------------------------------------------------
        // DEFAULT STATE
        // -----------------------------------------------------

        defaultPosition =
            position;

        defaultRotationZ =
            rotationZ;

        defaultStickerScale =
            stickerScale;

        defaultFlipX =
            flipX;

        defaultFlipY =
            flipY;

        // -----------------------------------------------------
        // LOADED STICKER
        // -----------------------------------------------------

        spawnedSticker = false;
        dragging = false;
    }

    // =========================================================
    // RESET COMPLET DU STICKER
    // =========================================================

    public void ResetStickerToDefault()
    {
        // -----------------------------------------------------
        // STOP DRAG
        // -----------------------------------------------------

        dragging = false;
        overDeleteZone = false;

        dragVelocity =
            Vector2.zero;

        // -----------------------------------------------------
        // POSITION
        // -----------------------------------------------------

        rect.anchoredPosition =
            defaultPosition;

        // -----------------------------------------------------
        // SCALE
        // -----------------------------------------------------

        stickerScale =
            defaultStickerScale;

        flipX =
            defaultFlipX;

        flipY =
            defaultFlipY;

        rect.localScale =
            new Vector3(
                GetScaleSignX() * stickerScale,
                GetScaleSignY() * stickerScale,
                1f
            );

        // -----------------------------------------------------
        // ROTATION
        // -----------------------------------------------------

        stickerRotationZ =
            defaultRotationZ;

        // -----------------------------------------------------
        // RESET TILT
        // -----------------------------------------------------

        currentTiltX = 0f;
        currentTiltY = 0f;

        rect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                stickerRotationZ
            );

        // -----------------------------------------------------
        // DRAG DATA
        // -----------------------------------------------------

        lastDragPosition =
            defaultPosition;

        offset =
            Vector2.zero;

        // -----------------------------------------------------
        // VISUAL
        // -----------------------------------------------------

        if (img != null)
        {
            img.color =
                baseColor;

            img.maskable =
                true;
        }

        // -----------------------------------------------------
        // UI
        // -----------------------------------------------------

        if (selected != null)
        {
            selected.SetActive(false);
        }

        if (anim != null)
        {
            anim.ResetTrigger("grab");
        }

        // -----------------------------------------------------
        // CURSOR
        // -----------------------------------------------------

        if (cursor != null)
        {
            cursor.SetHoverCursor();
        }
    }

    // =========================================================
    // SAVE VALUES FOR SAVE SYSTEM
    // =========================================================
    //
    // Ces fonctions permettent au SaveSystem de sauvegarder
    // les vraies valeurs et PAS le scale/tilt visuel Balatro.
    // =========================================================

    public float GetSaveRotationZ()
    {
        return stickerRotationZ;
    }

    public float GetSaveScaleX()
    {
        return GetScaleSignX() * stickerScale;
    }

    public float GetSaveScaleY()
    {
        return GetScaleSignY() * stickerScale;
    }
}