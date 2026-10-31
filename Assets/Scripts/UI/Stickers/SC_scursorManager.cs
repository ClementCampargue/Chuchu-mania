
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SC_scursorManager : MonoBehaviour
{
    public static SC_scursorManager instance;

    public Animator anim;

    [Header("Sprites curseur")]
    public Sprite normalSprite;
    public Sprite hoverSprite;
    public Sprite grabSprite;

    public bool cursor_enabled;

    [Header("UI Curseur")]
    public Image cursorRenderer;
    public RectTransform cursorRect;

    public bool grabing;

    [Header("Gamepad")]
    public InputActionReference cursorMoveAction;
    public InputActionReference cursorClickAction;

    public float gamepadCursorSpeed = 1200f;

    public Image img;
    public Image img2;

    private Mouse virtualMouse;
    private Vector2 cursorPosition;

    public Vector2 CursorPosition => cursorPosition;

    private void Awake()
    {
        instance = this;

        Cursor.visible = false;

        cursorPosition = new Vector2(
            Screen.width / 2f,
            Screen.height / 2f
        );

        if (cursorRect != null)
            cursorRect.position = cursorPosition;

        virtualMouse = InputSystem.AddDevice<Mouse>();

        InputSystem.EnableDevice(virtualMouse);

        InputState.Change(
            virtualMouse.position,
            cursorPosition
        );

        SetNormalCursor();
    }

    private void OnEnable()
    {
        if (cursorMoveAction != null)
            cursorMoveAction.action.Enable();

        if (cursorClickAction != null)
            cursorClickAction.action.Enable();
    }

    private void OnDisable()
    {
        if (cursorMoveAction != null)
            cursorMoveAction.action.Disable();

        if (cursorClickAction != null)
            cursorClickAction.action.Disable();
    }

    private void OnDestroy()
    {
        if (virtualMouse != null && virtualMouse.added)
            InputSystem.RemoveDevice(virtualMouse);

        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        bool usingController =
            SC_controller_manager.instance != null &&
            SC_controller_manager.instance.using_controller;

        bool isStickersScene =
            SceneManager.GetActiveScene().name == "Stickers";

        bool editing =
            isStickersScene &&
            SC_sticker_menu.instance != null &&
            SC_sticker_menu.instance.editing;

        // =================================
        // DÉPLACEMENT PERMANENT
        // =================================

        if (usingController)
        {
            MoveVirtualCursor();
        }
        else if (Mouse.current != null)
        {
            cursorPosition =
                Mouse.current.position.ReadValue();

            UpdateCursor();
        }

        // =================================
        // VISIBILITÉ
        // =================================

        if (isStickersScene)
        {
            // Dans Stickers : curseur visible en edit mode.
            // Hors edit : comportement normal.
            if (editing)
            {
                cursor_enabled = true;
                UpdateVisibility(true);
            }
            else
            {
                UpdateCursorMode();
                UpdateVisibility(cursor_enabled && !usingController);
            }
        }
        else
        {
            // Hors Stickers : comportement normal.
            UpdateCursorMode();

            UpdateVisibility(cursor_enabled && !usingController);
        }

        // =================================
        // ANIMATION CLIC
        // =================================

        if (!usingController &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (anim != null)
            {
                if (anim.enabled)
                    anim.SetTrigger("click");
                else
                    anim.enabled = true;
            }
        }
    }

    // =================================
    // MODE CURSEUR NORMAL
    // =================================

    public void UpdateCursorMode()
    {
        if (SC_controller_manager.instance == null)
            return;

        bool usingController =
            SC_controller_manager.instance.using_keyboard_controller;

        bool playerActive =
            SC_player.instance != null &&
            SC_player.instance.enabled &&
            SC_player.instance.gameObject.activeInHierarchy;

        if (usingController || playerActive)
        {
            cursor_enabled = false;
        }
        else
        {
            cursor_enabled = true;
        }

        Cursor.visible = false;
    }

    // =================================
    // SOURIS VIRTUELLE
    // =================================

    private void MoveVirtualCursor()
    {
        if (cursorMoveAction == null)
            return;

        Vector2 stick =
            cursorMoveAction.action.ReadValue<Vector2>();

        if (stick.sqrMagnitude < 0.01f)
            return;

        cursorPosition +=
            stick *
            gamepadCursorSpeed *
            Time.unscaledDeltaTime;

        cursorPosition.x = Mathf.Clamp(
            cursorPosition.x,
            0,
            Screen.width
        );

        cursorPosition.y = Mathf.Clamp(
            cursorPosition.y,
            0,
            Screen.height
        );

        UpdateCursor();
    }

    // =================================
    // AFFICHAGE
    // =================================

    private void UpdateVisibility(bool visible)
    {
        if (img != null)
            img.enabled = visible;

        if (img2 != null)
            img2.enabled = visible;
    }

    // =================================
    // POSITION CURSEUR
    // =================================

    public void UpdateCursor()
    {
        if (cursorRect != null)
            cursorRect.position = cursorPosition;

        if (virtualMouse != null)
        {
            InputState.Change(
                virtualMouse.position,
                cursorPosition
            );
        }
    }

    // =================================
    // SPRITES
    // =================================

    public void SetNormalCursor()
    {
        grabing = false;

        if (cursorRenderer != null)
            cursorRenderer.sprite = normalSprite;
    }

    public void SetHoverCursor()
    {
        grabing = false;

        if (cursorRenderer != null)
            cursorRenderer.sprite = hoverSprite;
    }

    public void SetGrabCursor()
    {
        grabing = true;

        if (cursorRenderer != null)
            cursorRenderer.sprite = grabSprite;
    }

    // =================================
    // ACTIVATION
    // =================================

    public void enable_cursor()
    {
        cursor_enabled = true;
        Cursor.visible = false;
    }

    public void disable_cursor()
    {
        cursor_enabled = false;
        Cursor.visible = false;
    }

    public void RecenterCursor()
    {
        cursorPosition = new Vector2(
            Screen.width / 2f,
            Screen.height / 2f
        );

        UpdateCursor();
    }

    // =================================
    // STICKERS
    // =================================

    public void UpdateStickerCursor(bool editing)
    {
        if (editing)
        {
            SetGrabCursor();
            cursor_enabled = true;
            UpdateVisibility(true);
        }
        else
        {
            UpdateCursorMode();

            bool usingController =
                SC_controller_manager.instance != null &&
                SC_controller_manager.instance.using_controller;

            UpdateVisibility(cursor_enabled && !usingController);
        }
    }
}
