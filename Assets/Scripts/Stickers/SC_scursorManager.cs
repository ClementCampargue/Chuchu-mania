using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public class SC_scursorManager : MonoBehaviour
{
    public static SC_scursorManager instance;

    public Animator anim;

    [Header("Sprites curseur")]
    public Sprite normalSprite;
    public Sprite hoverSprite;
    public Sprite grabSprite;



    [Header("UI Curseur")]
    public Image cursorRenderer;
    public RectTransform cursorRect;



    [Header("Gamepad")]
    public InputActionReference cursorMoveAction;
    public InputActionReference cursorClickAction;

    public float gamepadCursorSpeed = 1200f;



    private Mouse virtualMouse;

    private Vector2 cursorPosition;

    private bool usingGamepad;



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



    private void Start()
    {
        gameObject.SetActive(false);
    }



    void Update()
    {

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


        // =========================
        // SOURIS PHYSIQUE
        // =========================

        if (Mouse.current != null)
        {


            if (Mouse.current.delta.ReadValue()
                .sqrMagnitude > 0.01f)
            {
                usingGamepad = false;
            }



            if (!usingGamepad)
            {

                cursorPosition =
                    Mouse.current.position.ReadValue();


                UpdateCursor();
            }
        }







        // =========================
        // MANETTE
        // =========================


        Vector2 stick =
            cursorMoveAction.action
            .ReadValue<Vector2>();



        if (stick.sqrMagnitude > 0.01f)
        {

            usingGamepad = true;



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

}
