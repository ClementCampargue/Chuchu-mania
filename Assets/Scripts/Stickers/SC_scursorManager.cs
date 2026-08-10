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
        if(SceneManager.GetActiveScene().name != "Stickers")
        {
            if (cursor_enabled)
            {
                if (!SC_controller_manager.instance.using_controller)
                {
                    img2.enabled = true;
                    img.enabled = true;
                }
                else
                {
                    img2.enabled = false;
                    img.enabled = false;
                    return;
                }

            }
            else
            {
                img2.enabled = false;
                img.enabled = false;
                return;
            }
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                if (anim.enabled)
                {
                    anim.SetTrigger("click");
                }
                else
                {
                    anim.enabled = true;
                }
            }
        }
        else
        {
            cursor_enabled = true;
            img.enabled = true;
            img2.enabled = true;
        }



        // =========================
        // SOURIS PHYSIQUE
        // =========================

        if (Mouse.current != null)
        {


     

            if (!SC_controller_manager.instance.using_controller)
            {

                cursorPosition =
                    Mouse.current.position.ReadValue();


                UpdateCursor();
            }
        }







        // =========================
        // MANETTE
        // =========================

        if (SC_controller_manager.instance.using_controller)
        {

            Vector2 stick =
            cursorMoveAction.action
            .ReadValue<Vector2>();



            if (stick.sqrMagnitude > 0.01f)
            {

                cursorPosition +=
                    stick *
                    gamepadCursorSpeed *
                    Time.unscaledDeltaTime;



                cursorPosition.x =
                    Mathf.Clamp(
                        cursorPosition.x,
                        0,
                        Screen.width
                    );


                cursorPosition.y =
                    Mathf.Clamp(
                        cursorPosition.y,
                        0,
                        Screen.height
                    );



                UpdateCursor();
            }


        }



        // =========================
        // CLIC VIRTUEL
        // =========================

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
        if (cursorRenderer != null)
            cursorRenderer.sprite =
                normalSprite;
    }



    public void SetHoverCursor()
    {
        if (cursorRenderer != null)
            cursorRenderer.sprite =
                hoverSprite;
    }



    public void SetGrabCursor()
    {
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
