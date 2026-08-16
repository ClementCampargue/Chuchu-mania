using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
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




    // Position utilisée par tous les objets
    public Vector2 CursorPosition
    {
        get
        {
            return cursorPosition;
        }
    }



    void Awake()
    {

        instance = this;


        Cursor.visible = false;



        cursorPosition =
            new Vector2(
                Screen.width / 2f,
                Screen.height / 2f
            );



        cursorRect.position =
            cursorPosition;




        virtualMouse =
            InputSystem.AddDevice<Mouse>();



        InputSystem.EnableDevice(virtualMouse);




        InputState.Change(
            virtualMouse.position,
            cursorPosition
        );



        SetNormalCursor();

    }



    void OnEnable()
    {
        cursorMoveAction.action.Enable();
        cursorClickAction.action.Enable();
    }



    void OnDisable()
    {
        cursorMoveAction.action.Disable();
    }


    void Update()
    {
        bool usingController = SC_controller_manager.instance.using_controller;

        // =========================
        // VISIBILITÉ
        // =========================

        if (SceneManager.GetActiveScene().name != "Stickers")
        {

            if (!cursor_enabled)
            {
                img.enabled = false;
                img2.enabled = false;
                return;
            }

            if (usingController)
            {
                img.enabled = false;
                img2.enabled = false;
            }
            else
            {
                img.enabled = true;
                img2.enabled = true;
            }

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                if (anim.enabled)
                    anim.SetTrigger("click");
                else
                    anim.enabled = true;
            }
        }
        else
        {
            if (!cursor_enabled)
            {
                img.enabled = false;
                img2.enabled = false;
                return;
            }

            else
            {
                img.enabled = true;
                img2.enabled = true;
            }

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                if (anim.enabled)
                    anim.SetTrigger("click");
                else
                    anim.enabled = true;
            }
        }

        // =========================
        // SOURIS PHYSIQUE
        // =========================

        if (!usingController && Mouse.current != null)
            {
                cursorPosition = Mouse.current.position.ReadValue();

                UpdateCursor();
            }


        // =========================
        // MANETTE
        // =========================

        if (usingController)
        {
            Vector2 stick = cursorMoveAction.action.ReadValue<Vector2>();

            if (stick.sqrMagnitude > 0.1f)
            {
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
        }
    }


    void UpdateCursor()
    {

        // Visuel
        cursorRect.position =
            cursorPosition;



        // Souris UI virtuelle
        InputState.Change(
            virtualMouse.position,
            cursorPosition
        );

    }



    public void SetNormalCursor()
    {
        grabing = false;
        if (cursorRenderer != null)
            cursorRenderer.sprite =
                normalSprite;
    }



    public void SetHoverCursor()
    {
        grabing = false;

        if (cursorRenderer != null)
            cursorRenderer.sprite =
                hoverSprite;
    }



    public void SetGrabCursor()
    {
        grabing = true;
        if (cursorRenderer != null)
            cursorRenderer.sprite =
                grabSprite;
    }

    public void enable_cursor()
    {
        cursor_enabled = true;
    }

    public void disable_cursor()
    {
        cursor_enabled = false;
    }

}
