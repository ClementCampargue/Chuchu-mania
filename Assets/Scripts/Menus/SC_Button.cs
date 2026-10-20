using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class SC_Button : MonoBehaviour
{
    public bool select_on_enable;
    [Header("Animations")]
    public string hover;
    public string unhover;
    public string press;


    [Header("Events")]
    public UnityEvent onClick;
    public UnityEvent onHover;
    public UnityEvent onUnhover;

    private Animator anim;

    public bool isSelected;   // Navigation clavier/manette
    public bool isHovered;    // Souris
    public bool isPressed;
    public GameObject indicator;
    public bool accept_input = true;
    public SC_juiciness juice;
    public SC_juiciness juice2;
    public bool clickable = true;
    public SC_menu_navigation navigation;
    private void Awake()
    {
        anim = GetComponent<Animator>();
    }


    public void Select()
    {if (juice != null)
        {
            juice.PlayJuice();
        }
        isSelected = true;
        if (indicator != null)
        {
            indicator.SetActive(true);
        }
        Hover();
        PlayAnimation(hover);
    }

    private void OnDisable()
    {
        if (indicator != null)
        {
        indicator.SetActive(false);

        }
        OnUnhover();
        isSelected = false;
        isPressed = false;
        isHovered = false;
    }
    private void OnEnable()
    {
        if (select_on_enable && SC_controller_manager.instance.using_controller)
        {
            Select();
        }
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
            if (indicator != null)
            {
            indicator.SetActive(false);

            }
        }
    }


    private void OnMouseEnter()
    {
        if (SC_controller_manager.instance != null &&
            SC_controller_manager.instance.using_controller)
            return;
        if(navigation != null)
        {if(navigation.buttons.Length > 0)
            {
                foreach (SC_Button button in navigation.buttons)
                {
                    button.UnSelect();
                }
            }

        }
        if (!clickable)
            {
                return;
        }
        Hover();

        if (juice != null)
        {
            juice.PlayJuice();
        }

        isPressed = false;
        isHovered = true;
        if (indicator != null)
        {
        indicator.SetActive(true);

        }

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
        OnUnhover();
        isHovered = false;
        if (indicator != null)
        {
        indicator.SetActive(false);

        }

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
        if (indicator != null)
        {
        indicator.SetActive(false);

        }
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
        if (indicator != null)
        {
        indicator.SetActive(false);

        }
        if (juice2 != null)
        {
            juice2.PlayJuice();
        }
        PlayAnimation(press);
        Invoke("delay",0.5f);
    }
    void delay()
    {
        isPressed = false;
    }

    public void Click()
    {
      
        onClick?.Invoke();
    }

    public void Hover()
    {
        onHover?.Invoke();
    }

    public void OnUnhover()
    {
        if (!isPressed)
        {
            onUnhover?.Invoke();
        }
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
    private void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(
                col.bounds.center,
                col.bounds.size
            );
        }
    }
}