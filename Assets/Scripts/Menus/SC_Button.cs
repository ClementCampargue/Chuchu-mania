using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class SC_Button : MonoBehaviour
{
    [Header("Animations")]
    public string hover;
    public string unhover;
    public string press;

    [Header("Events")]
    public UnityEvent onClick;

    private Animator anim;

    private bool isSelected;   // Navigation clavier/manette
    public bool isHovered;    // Souris
    private bool isPressed;
    public GameObject indicator;
    public bool accept_input = true;
    public SC_juiciness juice;
    public SC_juiciness juice2;
    public bool clickable = true;
    private void Awake()
    {
        anim = GetComponent<Animator>();
    }


    public void Select()
    {
        juice.PlayJuice();
        isSelected = true;
        indicator.SetActive(true);
        PlayAnimation(hover);
    }

    private void OnDisable()
    {
        indicator.SetActive(false);
        isSelected = false;
        isPressed = false;
        isHovered = false;
    }

    // Appelé par EventSystem Deselect()
    public void UnSelect()
    {
        if (!isSelected)
            return;


        isSelected = false;

        // Si la souris est dessus, on garde l'état hover
        if (!isHovered)
        {
            PlayAnimation(unhover);
            indicator.SetActive(false);
        }
    }


    private void OnMouseEnter()
    {
        if (SC_controller_manager.instance != null &&
            SC_controller_manager.instance.using_controller)
            return;
        if (!clickable)
            {
                return;
            }
        juice.PlayJuice();

        isPressed = false;
        isHovered = true;
        indicator.SetActive(true);

        PlayAnimation(hover);
    }

    private void OnMouseExit()
    {
        if (!clickable)
        {
            return;
        }
        if (SC_controller_manager.instance != null &&
            SC_controller_manager.instance.using_controller)
            return;

        isHovered = false;
        indicator.SetActive(false);

        if (!isSelected)
        {
            PlayAnimation(unhover);
        }
    }

    private void OnMouseDown()
    {
        if (!clickable)
        {
            return;
        }
        if (SC_controller_manager.instance != null &&
            SC_controller_manager.instance.using_controller)
            return;

        PressAnimation();
    }

    private void OnMouseUp()
    {
        if (!clickable)
        {
            return;
        }
        if (SC_controller_manager.instance != null &&
            SC_controller_manager.instance.using_controller)
            return;

        if (isHovered)
        {
            Click();
        }

        indicator.SetActive(false);
        isPressed = false;
    }



    public void Press()
    {
        PressAnimation();
        Click();
    }


    private void PressAnimation()
    {
        if (isPressed)
            return;
        isPressed = true;
        indicator.SetActive(false);
        juice2.PlayJuice();
        PlayAnimation(press);
    }


    private void Click()
    {
        onClick?.Invoke();
    }


    private void PlayAnimation(string trigger)
    {
        if (anim == null || string.IsNullOrEmpty(trigger))
            return;
        anim.ResetTrigger("Hover");
        anim.ResetTrigger("Unhover");

        anim.ResetTrigger(trigger);
        anim.SetTrigger(trigger);
    }
}